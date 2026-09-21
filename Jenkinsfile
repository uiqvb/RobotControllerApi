// SIT223 7.3HD — RobotControllerApi CI/CD pipeline
// Manit Khera
//
// Seven stages: Build -> Test -> Code Quality -> Security -> Deploy -> Release -> Monitoring.
//
// Artefact model:
//   Stage 1 builds two images from the same Dockerfile - a deployable image from the
//   "production" target and a separate image from the "test" target that carries the
//   test project and tooling. The deployable image is built once and promoted unchanged
//   through test, scan, staging and production; everything after stage 1 consumes
//   ${IMAGE_TAG} rather than rebuilding.
//
// Host: Windows, Jenkins runs as a service, Docker Desktop with the WSL2 backend.
// Compose is v2+ so it is "docker compose", never "docker-compose".

// ===========================================================================
// HELPERS
// ===========================================================================

/**
 * Reads one field out of `docker inspect`.
 *
 * bat(returnStdout: true) returns the wrapper script's echo as well as the
 * command's output, so only the last non-empty line is the value we asked for.
 */
def dockerInspect(String target, String field) {
    def raw = bat(returnStdout: true,
                  script: "@echo off\r\ndocker inspect --format \"{{.${field}}}\" ${target}")
    return raw.readLines().findAll { it.trim() }.last().trim()
}

/**
 * Checks that `target` resolves to the image built in stage 1.
 *
 * Compares against the recorded image ID rather than the tag, because a tag is a
 * mutable label that a later build could move. Called at three points: the test
 * stack, staging and production.
 *
 * `field` is "Id" for an image and "Image" for a container - a container's .Image
 * is the ID of the image it was actually started from, so the post-deploy checks
 * test the running container rather than re-reading the tag.
 */
def verifyArtefact(String label, String target, String field) {
    def actual = dockerInspect(target, field)
    if (actual != env.IMAGE_ID) {
        error("""ARTEFACT MISMATCH at ${label}
  expected (built in stage 1) : ${env.IMAGE_ID}
  actual   (${target}) : ${actual}
Refusing to continue: this is not the image the pipeline built and tested.""")
    }
    echo "  [verified] ${label}: ${target} is running the stage-1 image"
}

/**
 * Polls an environment's /health endpoint until it reports healthy, or gives up
 * after 24 attempts at 5s intervals (~2 minutes).
 *
 * `portVar` is the name of an environment variable holding the port, so the same
 * helper serves staging and production, on both the deploy and rollback paths.
 * Returns true when healthy.
 */
def waitForHealthy(String portVar, String label) {
    return powershell(returnStatus: true, script: """
        \$url = "http://localhost:\$env:${portVar}/health"
        for (\$i = 0; \$i -lt 24; \$i++) {
            try {
                \$r = Invoke-WebRequest -Uri \$url -UseBasicParsing -TimeoutSec 5
                if (\$r.StatusCode -eq 200 -and \$r.Content -match '"status"\\s*:\\s*"healthy"') {
                    Write-Host "${label} healthy after \$(\$i*5)s: \$(\$r.Content)"
                    exit 0
                }
            } catch { }
            Start-Sleep -Seconds 5
        }
        Write-Host "${label} did not report healthy within 120s"
        exit 1
    """) == 0
}

/**
 * Handles a failed deployment validation. Always throws.
 *
 * With a previous known-good tag: redeploys it and verifies it becomes healthy.
 * Without one: stops the failed app container rather than leaving it serving.
 * Only the app service is touched - databases, volumes and networks are left alone.
 *
 * `cfg` keys: label, composeFile, envFile, portVar, stateFile, appService.
 */
def failDeployment(Map cfg, String reason) {
    def prev = fileExists(cfg.stateFile) ? readFile(file: cfg.stateFile).trim() : ''

    if (!prev) {
        bat """
            set IMAGE_TAG=${env.IMAGE_TAG}
            docker compose --env-file ${cfg.envFile} -f ${cfg.composeFile} stop ${cfg.appService}
        """
        error("""${cfg.label} deployment of ${env.IMAGE_TAG} failed verification.
  reason          : ${reason}
  rollback target : none recorded - this is the first deployment
  action taken    : failed ${cfg.label.toLowerCase()} app container stopped; database and volumes untouched
Manual investigation required.""")
    }

    echo "*** ${cfg.label.toUpperCase()} VERIFICATION FAILED - ROLLING BACK TO ${prev} ***"
    bat """
        set IMAGE_TAG=${prev}
        docker compose --env-file ${cfg.envFile} -f ${cfg.composeFile} up -d --force-recreate ${cfg.appService}
    """

    if (!waitForHealthy(cfg.portVar, "${cfg.label} rollback")) {
        error("""${cfg.label} deployment of ${env.IMAGE_TAG} failed verification AND the rollback to ${prev} did not become healthy.
  reason : ${reason}
Manual recovery required.""")
    }

    error("""${cfg.label} deployment of ${env.IMAGE_TAG} failed verification.
  reason         : ${reason}
  rolled back to : ${prev}, verified healthy""")
}

pipeline {
    agent any

    options {
        // Image tarballs are ~350MB each - keep the last 3 so the disk doesn't fill.
        buildDiscarder(logRotator(numToKeepStr: '15', artifactNumToKeepStr: '3'))
        timestamps()
        timeout(time: 45, unit: 'MINUTES')
        disableConcurrentBuilds()
    }

    triggers {
        // Jenkins cannot receive a GitHub webhook on localhost, so we poll.
        // Still zero manual steps: a push is the only trigger.
        pollSCM('H/2 * * * *')
    }

    environment {
        IMAGE_NAME      = 'myapp'
        IMAGE_TAG       = "1.0.${BUILD_NUMBER}"
        TEST_IMAGE_TAG  = "test-1.0.${BUILD_NUMBER}"

        STAGING_PORT    = '8090'
        PROD_PORT       = '8091'
        TEST_PORT       = '8092'

        // Where we remember the last known-good tag per environment, for rollback.
        DEPLOY_STATE    = 'C:\\ProgramData\\jenkins-deploy-state'

        // TODO: replace with the version verified working in this environment.
        // Check with: dotnet tool list --global   (after a successful build)
        SONAR_SCANNER_VERSION = '<SET_TO_CURRENT_WORKING_VERSION>'

        SONAR_PROJECT   = 'uiqvb_RobotControllerApi'
        SONAR_ORG       = 'uiqvb'
        SONAR_HOST      = 'https://sonarcloud.io'

        // Pinned: on :latest a Trivy release can change severity classification or
        // the fixed-version data, which moves the stage-4 gate between builds.
        TRIVY_IMAGE     = 'aquasec/trivy:0.58.1'

        // Floats within 8.0.x patch releases. Left unpinned deliberately - the
        // scanner only needs a matching major.minor SDK to analyse the build.
        DOTNET_SDK      = 'mcr.microsoft.com/dotnet/sdk:8.0'
    }

    stages {

        // =====================================================================
        // STAGE 1 - BUILD
        // Builds the deployable image (production target) plus a test image
        // (test target) used only by stage 2. Only the deployable image is
        // archived and promoted.
        // =====================================================================
        stage('Build') {
            steps {
                script {
                    // BRANCH_NAME is set only by multibranch jobs; GIT_BRANCH comes from
                    // the SCM checkout and is usually "origin/<name>". Neither is set on a
                    // detached checkout with no ref, hence the fallback.
                    def raw = env.BRANCH_NAME ?: env.GIT_BRANCH ?: ''
                    env.SOURCE_BRANCH = raw ? raw.replaceFirst(/^origin\//, '') : 'unknown'
                    echo "Building ${IMAGE_NAME}:${IMAGE_TAG} from branch ${env.SOURCE_BRANCH}"
                }

                bat """
                    if not exist "%DEPLOY_STATE%" mkdir "%DEPLOY_STATE%"

                    REM :latest is the fallback in docker-compose.*.yml
                    REM (image: myapp:${IMAGE_TAG:-latest}) for bringing a stack up by hand.
                    docker build --target production ^
                        -t %IMAGE_NAME%:%IMAGE_TAG% ^
                        -t %IMAGE_NAME%:latest ^
                        .

                    docker build --target test ^
                        -t %IMAGE_NAME%:%TEST_IMAGE_TAG% ^
                        .
                """

                // Archive the image itself, not just the build log, so the exact
                // artefact can be reloaded with `docker load` later.
                bat """
                    docker save %IMAGE_NAME%:%IMAGE_TAG% -o "%WORKSPACE%\\%IMAGE_NAME%-%IMAGE_TAG%.tar"
                    docker image inspect %IMAGE_NAME%:%IMAGE_TAG% --format "{{.Id}}" > "%WORKSPACE%\\image-id.txt"
                """

                archiveArtifacts artifacts: "${IMAGE_NAME}-${IMAGE_TAG}.tar, image-id.txt",
                                 fingerprint: true,
                                 onlyIfSuccessful: true

                bat 'docker image inspect %IMAGE_NAME%:%IMAGE_TAG% --format "Built {{.RepoTags}} / {{.Id}} / {{.Size}} bytes"'

                // Local image ID (the sha256 of the image config), not a registry digest -
                // nothing is pushed to a registry. Later stages compare against this rather
                // than the tag, which a subsequent build could move.
                script {
                    env.IMAGE_ID = dockerInspect("${IMAGE_NAME}:${IMAGE_TAG}", 'Id')
                    echo "Stage-1 image ID recorded: ${env.IMAGE_ID}"
                }
            }
            post {
                success { echo "Build OK - ${IMAGE_NAME}:${IMAGE_TAG} archived, image ID ${env.IMAGE_ID}" }
                failure { echo 'Build FAILED - no artefact produced, pipeline stops here' }
            }
        }

        // =====================================================================
        // STAGE 2 - TEST
        // Unit tests run inside the test image. Integration tests run against the
        // deployable image from stage 1, on its own compose stack.
        // A failed test aborts the pipeline. A run that executes zero tests is also
        // a failure: dotnet test exits 0 when it finds no tests, which would
        // otherwise report a false pass.
        // =====================================================================
        stage('Test') {
            steps {
                bat 'if not exist "%WORKSPACE%\\testresults" mkdir "%WORKSPACE%\\testresults"'

                echo '--- Unit tests (inside the test image) ---'
                bat """
                    docker run --rm ^
                        -v "%WORKSPACE%\\testresults:/testresults" ^
                        %IMAGE_NAME%:%TEST_IMAGE_TAG% ^
                        dotnet test --filter "Category!=Integration" ^
                            --logger "junit;LogFilePath=/testresults/unit-results.xml" ^
                            --results-directory /testresults
                """

                echo '--- Bringing up the test stack on the stage-1 deployable image ---'
                bat """
                    set IMAGE_TAG=%IMAGE_TAG%
                    docker compose -f docker-compose.test.yml up -d
                """

                powershell '''
                    $url = "http://localhost:$env:TEST_PORT/health"
                    $ok = $false
                    for ($i = 0; $i -lt 30; $i++) {
                        try {
                            $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
                            if ($r.StatusCode -eq 200) { $ok = $true; break }
                        } catch { }
                        Start-Sleep -Seconds 5
                    }
                    if (-not $ok) { Write-Error "Test stack never became healthy at $url"; exit 1 }
                    Write-Host "Test stack healthy after $($i * 5)s"
                '''

                // Image check 1 of 3: confirms the integration tests below run against
                // the deployable image rather than some other build of it.
                script { verifyArtefact('test stack', 'robot-test-app', 'Image') }

                echo '--- Integration tests (against the deployable image) ---'
                // Joins the test stack's network so the test image can reach the app by
                // compose service name, which is what API_BASE_URL defaults to. The test
                // project is already compiled into the image, so --no-build skips a
                // recompile.
                bat """
                    docker run --rm ^
                        --network robot-test-net ^
                        -v "%WORKSPACE%\\testresults:/testresults" ^
                        %IMAGE_NAME%:%TEST_IMAGE_TAG% ^
                        dotnet test -c Release --no-build --filter "Category=Integration" ^
                            --logger "junit;LogFilePath=/testresults/integration-results.xml" ^
                            --results-directory /testresults
                """

                // Fails the stage if no tests ran - see the zero-test note above.
                powershell '''
                    $files = Get-ChildItem "$env:WORKSPACE\\testresults" -Filter *.xml -ErrorAction SilentlyContinue
                    if (-not $files) { Write-Error "No test result files produced"; exit 1 }
                    $total = 0
                    foreach ($f in $files) {
                        [xml]$x = Get-Content $f.FullName
                        $n = 0
                        $x.SelectNodes("//testsuite") | ForEach-Object { $n += [int]$_.tests }
                        Write-Host "$($f.Name): $n tests"
                        $total += $n
                    }
                    Write-Host "TOTAL TESTS EXECUTED: $total"
                    if ($total -eq 0) {
                        Write-Error "Zero tests executed. dotnet test exits 0 when it finds no tests, so this is failed explicitly."
                        exit 1
                    }
                    # Recorded for the build manifest written in stage 7.
                    Set-Content -Path "$env:WORKSPACE\\testresults\\test-count.txt" -Value $total -Encoding ascii
                '''
            }
            post {
                always {
                    junit allowEmptyResults: false, testResults: 'testresults/*.xml'
                    bat 'docker compose -f docker-compose.test.yml down -v || exit /b 0'
                }
                success { echo 'Tests PASSED - unit and integration, gated' }
                failure { echo 'Tests FAILED - pipeline stops, nothing is deployed' }
            }
        }

        // =====================================================================
        // STAGE 3 - CODE QUALITY
        // Maintainability analysis: duplication, code smells, complexity. Separate
        // from the vulnerability scanning in stage 4.
        // Uses dotnet-sonarscanner (the MSBuild scanner) rather than the CLI scanner
        // used in 7.1C, which cannot see the .NET compilation and reports very little.
        // It needs the SDK, so the analysis runs inside a .NET SDK container.
        // A failing quality gate fails the stage.
        // =====================================================================
        stage('Code Quality') {
            steps {
                // Written to a file rather than passed as an inline `bash -c`: with three
                // layers of quoting (Groovy -> cmd -> bash), Groovy interpolates $PATH and
                // the Windows PATH ends up inside the container, hiding dotnet. A script
                // file has one layer of quoting and needs no escaping.
                writeFile file: 'sonar-scan.sh', text: '''#!/bin/bash
set -euo pipefail

# dotnet-sonarscanner is a Java application. The .NET SDK image ships no JRE,
# so install a headless one before doing anything else.
echo "--- installing JRE for the scanner ---"
apt-get update -qq
apt-get install -y -qq --no-install-recommends openjdk-17-jre-headless > /dev/null
java -version

echo "--- copying source out of the bind mount ---"
# MSBuild cannot write bin/ into a Windows bind mount from inside a Linux
# container - it fails with MSB3021 "Access to the path is denied" on every
# Content file it tries to copy. The analysis therefore runs against a copy on
# the container's own filesystem. Nothing needs to come back to the host: the
# scanner uploads its results to SonarCloud directly.
mkdir -p /build
cp -a /src/. /build/
cd /build
# Build leftovers, the archived image tarball, and monitoring config, which is
# infrastructure rather than application content and would otherwise be pulled
# into the compile as a Content item.
rm -rf /build/bin /build/obj /build/.sonarqube /build/testresults /build/monitoring
rm -f /build/*.tar
ls -la /build | head -30

echo "--- installing dotnet-sonarscanner ---"
dotnet tool install --global dotnet-sonarscanner --version "$SONAR_SCANNER_VERSION"
export PATH="$PATH:/root/.dotnet/tools"

echo "--- sonarscanner begin ---"
dotnet sonarscanner begin \\
    /k:"$SONAR_PROJECT" \\
    /o:"$SONAR_ORG" \\
    /d:sonar.host.url="$SONAR_HOST" \\
    /d:sonar.token="$SONAR_TOKEN" \\
    /d:sonar.exclusions="**/bin/**,**/obj/**,**/Migrations/**,**/*.sql,**/wwwroot/**,**/TestResults/**" \\
    /d:sonar.qualitygate.wait=true

echo "--- build under analysis ---"
dotnet build --no-incremental

# sonar.qualitygate.wait=true was set at begin, so this call blocks until
# SonarCloud returns the gate result and exits non-zero if it failed.
echo "--- sonarscanner end ---"
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
'''
                // Single-quoted Groovy string: no interpolation, so the token is not baked
                // into the command line. cmd expands the %VARS%, and the secret reaches
                // the container only as an inherited environment variable via -e.
                withCredentials([string(credentialsId: 'SONAR_TOKEN', variable: 'SONAR_TOKEN')]) {
                    bat '''
                        docker run --rm ^
                            -v "%WORKSPACE%:/src" ^
                            -w /src ^
                            -e SONAR_TOKEN ^
                            -e SONAR_PROJECT=%SONAR_PROJECT% ^
                            -e SONAR_ORG=%SONAR_ORG% ^
                            -e SONAR_HOST=%SONAR_HOST% ^
                            -e SONAR_SCANNER_VERSION=%SONAR_SCANNER_VERSION% ^
                            %DOTNET_SDK% ^
                            bash /src/sonar-scan.sh
                    '''
                }
                echo "Quality gate passed. Trend: ${SONAR_HOST}/project/activity?id=${SONAR_PROJECT}"
            }
            post {
                always  { bat 'del /Q "%WORKSPACE%\\sonar-scan.sh" 2>nul || exit /b 0' }
                failure {
                    echo '''CODE QUALITY STAGE FAILED. Likely causes, in order:
  1. SonarCloud project does not exist yet, or the key/org is wrong
  2. Automatic Analysis is still enabled on the SonarCloud project
  3. The quality gate genuinely failed - check the dashboard'''
                }
            }
        }

        // =====================================================================
        // STAGE 4 - SECURITY
        // Trivy scans the built image: OS packages, NuGet dependencies and secrets.
        // Separate concern from stage 3's maintainability analysis.
        //
        // Gate: fails the stage on CRITICAL vulnerabilities that have a fix available
        // (--ignore-unfixed). Unfixable CRITICALs are reported and archived but do not
        // fail the build - their disposition is recorded in security-findings.md.
        // =====================================================================
        stage('Security') {
            steps {
                bat 'if not exist "%WORKSPACE%\\security" mkdir "%WORKSPACE%\\security"'

                // Report-only pass: `|| exit /b 0` keeps the scan output available even
                // when the gate below fails the stage.
                bat """
                    docker run --rm ^
                        -v //var/run/docker.sock:/var/run/docker.sock ^
                        -v "%WORKSPACE%\\security:/out" ^
                        %TRIVY_IMAGE% image ^
                        --scanners vuln,secret ^
                        --severity LOW,MEDIUM,HIGH,CRITICAL ^
                        --format table ^
                        --output /out/trivy-full-report.txt ^
                        %IMAGE_NAME%:%IMAGE_TAG% || exit /b 0
                """

                bat """
                    docker run --rm ^
                        -v //var/run/docker.sock:/var/run/docker.sock ^
                        -v "%WORKSPACE%\\security:/out" ^
                        %TRIVY_IMAGE% image ^
                        --scanners vuln,secret ^
                        --format json ^
                        --output /out/trivy-report.json ^
                        %IMAGE_NAME%:%IMAGE_TAG% || exit /b 0
                """

                bat 'type "%WORKSPACE%\\security\\trivy-full-report.txt"'

                // Config scan: catches Dockerfile/compose misconfiguration (running as
                // root, missing USER, writable root filesystem) that an image scan does
                // not look for. Report-only - no baseline has been triaged yet, so
                // gating on it would fail builds on untriaged findings.
                bat """
                    docker run --rm ^
                        -v "%WORKSPACE%:/project" ^
                        -v "%WORKSPACE%\\security:/out" ^
                        %TRIVY_IMAGE% config /project ^
                        --severity HIGH,CRITICAL ^
                        --format table ^
                        --output /out/trivy-config-report.txt || exit /b 0
                """
                bat 'type "%WORKSPACE%\\security\\trivy-config-report.txt" 2>nul || echo (no misconfiguration findings)'

                // Summarises the JSON report by severity, splitting fixable from
                // unfixable. Only fixable CRITICALs are actionable, and those are what
                // the gate below blocks on. Also writes trivy-summary.json, which stage 7
                // splices into the build manifest.
                powershell '''
                    $path = "$env:WORKSPACE\\security\\trivy-report.json"
                    $j = Get-Content $path -Raw | ConvertFrom-Json

                    $vulns = @()
                    $secretCount = 0
                    foreach ($r in $j.Results) {
                        if ($r.Vulnerabilities) { $vulns += $r.Vulnerabilities }
                        if ($r.Secrets)         { $secretCount += $r.Secrets.Count }
                    }

                    Write-Host ""
                    Write-Host "================ SECURITY SUMMARY ================"
                    Write-Host ("  Image scanned : {0}:{1}" -f $env:IMAGE_NAME, $env:IMAGE_TAG)
                    Write-Host ("  Total findings: {0}" -f $vulns.Count)
                    Write-Host ""
                    Write-Host "  Severity      Total   Fixable"
                    foreach ($sev in @("CRITICAL","HIGH","MEDIUM","LOW","UNKNOWN")) {
                        $s = @($vulns | Where-Object { $_.Severity -eq $sev })
                        $f = @($s      | Where-Object { $_.FixedVersion })
                        if ($s.Count -gt 0) {
                            Write-Host ("  {0,-12} {1,6} {2,9}" -f $sev, $s.Count, $f.Count)
                        }
                    }

                    $appVulns = 0
                    foreach ($r in $j.Results) {
                        if ($r.Class -eq "lang-pkgs" -and $r.Vulnerabilities) {
                            $appVulns += $r.Vulnerabilities.Count
                        }
                    }
                    $fixableCritical = @($vulns | Where-Object { $_.Severity -eq "CRITICAL" -and $_.FixedVersion }).Count

                    Write-Host ""
                    Write-Host ("  Application dependency findings : {0}" -f $appVulns)
                    Write-Host ("  Secrets embedded in the image   : {0}" -f $secretCount)
                    Write-Host ("  CRITICAL with a fix available   : {0}   <- this is what the gate blocks on" -f $fixableCritical)
                    Write-Host "=================================================="
                    Write-Host ""

                    $summary = [ordered]@{
                        image              = "$($env:IMAGE_NAME):$($env:IMAGE_TAG)"
                        scannedAtUtc       = (Get-Date).ToUniversalTime().ToString("o")
                        totalFindings      = $vulns.Count
                        critical           = @($vulns | Where-Object { $_.Severity -eq "CRITICAL" }).Count
                        high               = @($vulns | Where-Object { $_.Severity -eq "HIGH" }).Count
                        medium             = @($vulns | Where-Object { $_.Severity -eq "MEDIUM" }).Count
                        low                = @($vulns | Where-Object { $_.Severity -eq "LOW" }).Count
                        fixableCritical    = $fixableCritical
                        fixableAnySeverity = @($vulns | Where-Object { $_.FixedVersion }).Count
                        appDependencyFindings = $appVulns
                        secretsFound       = $secretCount
                    }
                    $summary | ConvertTo-Json | Set-Content "$env:WORKSPACE\\security\\trivy-summary.json" -Encoding ascii
                '''

                // Gate: exits 1 on CRITICAL vulnerabilities that have a fix available.
                // --ignore-unfixed means unfixable CRITICALs do not fail the stage.
                bat """
                    docker run --rm ^
                        -v //var/run/docker.sock:/var/run/docker.sock ^
                        %TRIVY_IMAGE% image ^
                        --scanners vuln ^
                        --severity CRITICAL ^
                        --exit-code 1 ^
                        --ignore-unfixed ^
                        %IMAGE_NAME%:%IMAGE_TAG%
                """

                // security-findings.md is documentation, not a pipeline input. Archived
                // alongside the scan output when present; its absence is not a failure.
                archiveArtifacts artifacts: 'security/trivy-full-report.txt, security/trivy-report.json, security/trivy-summary.json, security/trivy-config-report.txt, security-findings.md',
                                 allowEmptyArchive: true
            }
            post {
                always  { echo 'Scan reports archived. Dispositions are recorded in security-findings.md.' }
                success { echo 'Security gate PASSED - no CRITICAL vulnerability with an available fix' }
                failure {
                    echo 'CRITICAL vulnerability with an available fix. Update the dependency, or document the mitigation in security-findings.md, then re-run.'
                }
            }
        }

        // =====================================================================
        // STAGE 5 - DEPLOY (staging)
        // The committed compose file is the only description of the environment.
        // Environment-specific config comes from the ENV_STAGING Jenkins credential,
        // never from the repo.
        //
        // On a failed health check the stage redeploys the previous known-good tag
        // and verifies it before failing. Rollback depends on that tag's image still
        // being present locally - see the note on image pruning in the final post block.
        // =====================================================================
        stage('Deploy to Staging') {
            steps {
                withCredentials([file(credentialsId: 'ENV_STAGING', variable: 'ENV_FILE')]) {
                    bat 'copy /Y "%ENV_FILE%" "%WORKSPACE%\\.env.staging"'
                }

                script {
                    // On the first ever deploy there is no previous tag to roll back to,
                    // so the state file will not exist yet.
                    def stateFile = "${DEPLOY_STATE}\\staging-last-good.txt"
                    def prev = fileExists(stateFile) ? readFile(file: stateFile).trim() : ''
                    echo "Previous known-good staging tag: ${prev ?: 'none - this is the first deploy'}"
                }

                bat """
                    set IMAGE_TAG=%IMAGE_TAG%
                    docker compose --env-file .env.staging -f docker-compose.staging.yml up -d
                """

                script {
                    def cfg = [label      : 'Staging',
                               composeFile: 'docker-compose.staging.yml',
                               envFile    : '.env.staging',
                               portVar    : 'STAGING_PORT',
                               stateFile  : "${DEPLOY_STATE}\\staging-last-good.txt",
                               appService : 'app']

                    // Health, image identity and smoke checks are one validation: any
                    // failure rolls back. The tag is recorded as known-good only after
                    // all of them pass, so a bad release can never become a rollback target.
                    def failure = null
                    try {
                        if (!waitForHealthy('STAGING_PORT', 'Staging')) {
                            failure = 'health endpoint did not report healthy'
                        } else {
                            // Image check 2 of 3.
                            verifyArtefact('staging', 'robot-staging-app', 'Image')

                            // A health check confirms the app is listening and can reach its
                            // database. It does not confirm which application is behind the
                            // port or that authentication survived the deployment. These
                            // assertions are read-only and need no credentials.
                            powershell '''
                        $base = "http://localhost:$env:STAGING_PORT"
                        $failures = @()

                        Write-Host "--- Smoke test: staging ---"

                        # 1. The application answers, and it can really reach its database.
                        try {
                            $r = Invoke-WebRequest "$base/health" -UseBasicParsing -TimeoutSec 10
                            if ($r.StatusCode -ne 200)                       { $failures += "health returned $($r.StatusCode), expected 200" }
                            elseif ($r.Content -notmatch '"database"\\s*:\\s*"connected"') { $failures += "health did not report a connected database: $($r.Content)" }
                            else { Write-Host "  [pass] /health -> 200, database connected" }
                        } catch { $failures += "health request threw: $($_.Exception.Message)" }

                        # 2. Authorisation fails closed. A deployment that dropped
                        #    authentication would still pass the health check.
                        try {
                            Invoke-WebRequest "$base/api/maps" -UseBasicParsing -TimeoutSec 10 | Out-Null
                            $failures += "GET /api/maps without credentials returned 200 - authentication is NOT enforced"
                        } catch {
                            $code = $_.Exception.Response.StatusCode.value__
                            if ($code -eq 401) { Write-Host "  [pass] unauthenticated /api/maps -> 401, auth fails closed" }
                            else               { $failures += "unauthenticated /api/maps returned $code, expected 401" }
                        }

                        # 3. The container's own HEALTHCHECK, which is Docker probing from
                        #    inside and is what compose dependency ordering uses.
                        #    The image sets --start-period=20s --interval=15s, so a
                        #    freshly recreated container reports "starting" until the
                        #    first probe runs. Poll until it reaches a verdict.
                        $state = "starting"
                        for ($i = 0; $i -lt 24; $i++) {
                            $state = ((docker inspect --format "{{.State.Health.Status}}" robot-staging-app) | Select-Object -Last 1).Trim()
                            if ($state -ne "starting") { break }
                            Start-Sleep -Seconds 5
                        }
                        if ($state -eq "healthy") { Write-Host "  [pass] container HEALTHCHECK reports healthy (after $($i*5)s)" }
                        else { $failures += "container health status is '$state', expected 'healthy'" }

                        if ($failures.Count -gt 0) {
                            Write-Host ""
                            Write-Host "SMOKE TEST FAILED:"
                            $failures | ForEach-Object { Write-Host "  - $_" }
                            exit 1
                        }
                        Write-Host "Smoke test passed: 3/3"
                    '''
                        }
                    } catch (Exception err) {
                        failure = err.getMessage()
                    }

                    if (failure) { failDeployment(cfg, failure) }

                    writeFile file: cfg.stateFile, text: "${IMAGE_TAG}"
                    echo "Staging validated - ${IMAGE_TAG} recorded as last known-good"
                }

                bat 'docker compose --env-file .env.staging -f docker-compose.staging.yml ps'
            }
            post {
                success { echo "Staging is running ${IMAGE_TAG} on port ${STAGING_PORT} and is healthy" }
                always  { bat 'del /Q "%WORKSPACE%\\.env.staging" 2>nul || exit /b 0' }
            }
        }

        // =====================================================================
        // STAGE 6 - RELEASE (production)
        // Promotes the image staging just verified - no rebuild. Production is a
        // separate environment: own database, volume, network, port and config,
        // supplied by the ENV_PROD credential. Releases are tagged in git.
        //
        // Same rollback contract as staging: redeploy the previous known-good tag
        // and verify it before failing.
        // =====================================================================
        stage('Release to Production') {
            steps {
                withCredentials([file(credentialsId: 'ENV_PROD', variable: 'ENV_FILE')]) {
                    bat 'copy /Y "%ENV_FILE%" "%WORKSPACE%\\.env.prod"'
                }

                bat """
                    set IMAGE_TAG=%IMAGE_TAG%
                    docker compose --env-file .env.prod -f docker-compose.prod.yml up -d
                """

                script {
                    def cfg = [label      : 'Production',
                               composeFile: 'docker-compose.prod.yml',
                               envFile    : '.env.prod',
                               portVar    : 'PROD_PORT',
                               stateFile  : "${DEPLOY_STATE}\\prod-last-good.txt",
                               appService : 'app']

                    // Health, image identity, smoke checks and environment isolation are
                    // one validation. Any failure rolls back production to its own last
                    // known-good tag; staging is not touched.
                    def failure = null
                    try {
                        if (!waitForHealthy('PROD_PORT', 'Production')) {
                            failure = 'health endpoint did not report healthy'
                        } else {
                            // Image check 3 of 3: confirms production is running the image
                            // built in stage 1 and tested in stage 2.
                            verifyArtefact('production', 'robot-prod-app', 'Image')

                            // The same read-only assertions as staging, repeated here because
                            // staging passing only shows the image is good - it says nothing
                            // about production's own config, database and network.
                            powershell '''
                        $base = "http://localhost:$env:PROD_PORT"
                        $failures = @()

                        Write-Host "--- Smoke test: production ---"

                        try {
                            $r = Invoke-WebRequest "$base/health" -UseBasicParsing -TimeoutSec 10
                            if ($r.StatusCode -ne 200)                       { $failures += "health returned $($r.StatusCode), expected 200" }
                            elseif ($r.Content -notmatch '"database"\\s*:\\s*"connected"') { $failures += "health did not report a connected database: $($r.Content)" }
                            else { Write-Host "  [pass] /health -> 200, database connected" }
                        } catch { $failures += "health request threw: $($_.Exception.Message)" }

                        try {
                            Invoke-WebRequest "$base/api/maps" -UseBasicParsing -TimeoutSec 10 | Out-Null
                            $failures += "GET /api/maps without credentials returned 200 - authentication is NOT enforced in production"
                        } catch {
                            $code = $_.Exception.Response.StatusCode.value__
                            if ($code -eq 401) { Write-Host "  [pass] unauthenticated /api/maps -> 401, auth fails closed" }
                            else               { $failures += "unauthenticated /api/maps returned $code, expected 401" }
                        }

                        # Same start-period wait as staging - see the comment there.
                        $state = "starting"
                        for ($i = 0; $i -lt 24; $i++) {
                            $state = ((docker inspect --format "{{.State.Health.Status}}" robot-prod-app) | Select-Object -Last 1).Trim()
                            if ($state -ne "starting") { break }
                            Start-Sleep -Seconds 5
                        }
                        if ($state -eq "healthy") { Write-Host "  [pass] container HEALTHCHECK reports healthy (after $($i*5)s)" }
                        else { $failures += "container health status is '$state', expected 'healthy'" }

                        if ($failures.Count -gt 0) {
                            Write-Host ""
                            Write-Host "PRODUCTION SMOKE TEST FAILED:"
                            $failures | ForEach-Object { Write-Host "  - $_" }
                            exit 1
                        }
                        Write-Host "Smoke test passed: 3/3"
                    '''

                            // Environment isolation check. If a config error pointed staging
                            // and production at the same database or volume, every check
                            // above would still pass and the fault would only surface later
                            // as corrupted data.
                            powershell '''
                        $s = (docker inspect --format "{{.Id}}" robot-staging-db) | Select-Object -Last 1
                        $p = (docker inspect --format "{{.Id}}" robot-prod-db)    | Select-Object -Last 1
                        if ($s.Trim() -eq $p.Trim()) {
                            Write-Error "Staging and production are sharing a database container - environments are NOT isolated"
                            exit 1
                        }
                        $sv = (docker inspect --format "{{range .Mounts}}{{.Name}} {{end}}" robot-staging-db) | Select-Object -Last 1
                        $pv = (docker inspect --format "{{range .Mounts}}{{.Name}} {{end}}" robot-prod-db)    | Select-Object -Last 1
                        Write-Host "  [pass] separate database containers"
                        Write-Host "         staging volume(s)   : $($sv.Trim())"
                        Write-Host "         production volume(s): $($pv.Trim())"
                        if ($sv.Trim() -eq $pv.Trim()) {
                            Write-Error "Both environments are mounting the same volume - data is NOT isolated"
                            exit 1
                        }
                    '''
                        }
                    } catch (Exception err) {
                        failure = err.getMessage()
                    }

                    if (failure) { failDeployment(cfg, failure) }

                    writeFile file: cfg.stateFile, text: "${IMAGE_TAG}"
                    echo "Production validated - ${IMAGE_TAG} recorded as last known-good"
                }

                // Tag the released commit. @echo off keeps the token out of the console:
                // Jenkins masks credentials, but the push URL is not echoed at all this way.
                withCredentials([usernamePassword(credentialsId: 'GITHUB_CREDS',
                                                  usernameVariable: 'GIT_USER',
                                                  passwordVariable: 'GIT_TOKEN')]) {
                    bat """
                        @echo off
                        git config user.email "manitkhera26@gmail.com"
                        git config user.name "Jenkins"
                        git tag -a v%IMAGE_TAG% -m "Automated release v%IMAGE_TAG% from build #%BUILD_NUMBER%"
                        git push https://%GIT_USER%:%GIT_TOKEN%@github.com/uiqvb/RobotControllerApi.git v%IMAGE_TAG%
                    """
                }

                bat 'docker compose --env-file .env.prod -f docker-compose.prod.yml ps'
            }
            post {
                success { echo "RELEASED v${IMAGE_TAG} to production on port ${PROD_PORT}" }
                always  { bat 'del /Q "%WORKSPACE%\\.env.prod" 2>nul || exit /b 0' }
            }
        }

        // =====================================================================
        // STAGE 7 - MONITORING & ALERTING
        // Prometheus scrapes cAdvisor (container metrics) and blackbox_exporter
        // (probes production's /health for uptime and latency). Grafana renders it.
        // Alertmanager fires on container-down, health-probe-failing and
        // high-latency rules.
        // =====================================================================
        stage('Monitoring') {
            steps {
                bat """
                    docker compose -f monitoring/docker-compose.monitoring.yml up -d
                """

                // Job names come from monitoring/prometheus.yml. A plain count would pass
                // with the wrong targets up - cadvisor supplies container metrics and
                // blackbox-health probes staging and production, and the alert rules
                // depend on both.
                powershell '''
                    $required = @("cadvisor", "blackbox-health")
                    $ok = $false
                    for ($i = 0; $i -lt 24; $i++) {
                        try {
                            $r = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets" -TimeoutSec 5
                            $active = $r.data.activeTargets
                            $missing = @()
                            foreach ($job in $required) {
                                $t = @($active | Where-Object { $_.labels.job -eq $job })
                                if ($t.Count -eq 0)                                   { $missing += "$job (no targets)" }
                                elseif (@($t | Where-Object { $_.health -ne "up" })) { $missing += "$job (not up)" }
                            }
                            if ($missing.Count -eq 0) { $ok = $true; break }
                        } catch { }
                        Start-Sleep -Seconds 5
                    }
                    if (-not $ok) {
                        Write-Error "Required Prometheus targets not up: $($missing -join ', ')"
                        exit 1
                    }
                    Write-Host "Required Prometheus targets are up:"
                    $active | ForEach-Object {
                        Write-Host ("  {0,-18} {1,-6} {2}" -f $_.labels.job, $_.health, $_.labels.environment)
                    }
                '''

                // Target health only proves Prometheus can scrape blackbox_exporter. This
                // checks the production probe itself is succeeding, which is what
                // ProductionDown actually alerts on.
                powershell '''
                    $ok = $false
                    for ($i = 0; $i -lt 12; $i++) {
                        try {
                            $q = Invoke-RestMethod -TimeoutSec 5 -Uri ("http://localhost:9090/api/v1/query?query=" +
                                 [uri]::EscapeDataString('probe_success{environment="production"}'))
                            $v = @($q.data.result)
                            if ($v.Count -gt 0 -and $v[0].value[1] -eq "1") { $ok = $true; break }
                        } catch { }
                        Start-Sleep -Seconds 5
                    }
                    if (-not $ok) { Write-Error "Prometheus is not reporting a successful production health probe"; exit 1 }
                    Write-Host "  [pass] production /health probe succeeding"
                '''

                // Compose compares mount specs, not file contents, so it will not recreate
                // a container when only a bind-mounted config file changed. Without this,
                // an edit to prometheus.yml, alert-rules.yml or alertmanager.yml is
                // committed and deployed but ignored by the running process. Both services
                // expose a reload endpoint; failing here rather than continuing prevents
                // the stage passing on a stale config.
                powershell '''
                    $targets = @(
                        @{ name = "prometheus";   url = "http://localhost:9090/-/reload" },
                        @{ name = "alertmanager"; url = "http://localhost:9093/-/reload" }
                    )
                    foreach ($t in $targets) {
                        $done = $false
                        for ($i = 0; $i -lt 6; $i++) {
                            try {
                                Invoke-WebRequest -Uri $t.url -Method Post -UseBasicParsing -TimeoutSec 10 | Out-Null
                                Write-Host "  reloaded $($t.name) config"
                                $done = $true
                                break
                            } catch { Start-Sleep -Seconds 5 }
                        }
                        if (-not $done) { Write-Error "Could not reload $($t.name) - it may be running a stale config"; exit 1 }
                    }
                '''

                powershell '''
                    $rules = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/rules" -TimeoutSec 10
                    Write-Host "Loaded alert rules:"
                    foreach ($g in $rules.data.groups) {
                        foreach ($r in $g.rules) { Write-Host "  [$($g.name)] $($r.name) - state: $($r.state)" }
                    }
                '''

                // Confirms the notification receiver is up. Prometheus evaluating a rule
                // and Alertmanager delivering the notification are separate steps, and a
                // rules check alone would pass with the delivery end down.
                powershell '''
                    $ok = $false
                    for ($i = 0; $i -lt 12; $i++) {
                        try {
                            $r = Invoke-RestMethod -Uri "http://localhost:5001/" -TimeoutSec 5
                            if ($r.status -eq "ready") { $ok = $true; break }
                        } catch { }
                        Start-Sleep -Seconds 5
                    }
                    if (-not $ok) { Write-Error "Alert notification receiver is not reachable - alerts would fire into nothing"; exit 1 }
                    Write-Host "Notification receiver ready. Deliveries are visible with: docker logs robot-alert-logger"
                '''

                // End-to-end notification check. Posts a clearly-named CI alert straight
                // to Alertmanager and confirms the receiver logged it, proving routing and
                // delivery without stopping a real service. Resolved immediately afterwards
                // so nothing is left firing. Does not exercise Prometheus rule evaluation -
                // that path is demonstrated separately by stopping the production container.
                powershell '''
                    $amUrl = "http://localhost:9093/api/v2/alerts"
                    $now   = (Get-Date).ToUniversalTime()

                    function Send-CIAlert([datetime]$ends) {
                        $a = @{
                            labels      = @{ alertname   = "CISyntheticAlert"
                                             severity    = "info"
                                             environment = "ci"
                                             origin      = "jenkins-pipeline" }
                            annotations = @{ summary = "CI notification-path verification. Not a real incident." }
                            startsAt    = $now.ToString("o")
                            endsAt      = $ends.ToString("o")
                        }
                        # ConvertTo-Json unwraps a single-element array; Alertmanager needs a list.
                        $body = "[" + ($a | ConvertTo-Json -Depth 6 -Compress) + "]"
                        Invoke-RestMethod -Uri $amUrl -Method Post -Body $body -ContentType "application/json" -TimeoutSec 10 | Out-Null
                    }

                    Send-CIAlert $now.AddMinutes(3)
                    Write-Host "Synthetic alert posted to Alertmanager, waiting for delivery..."

                    $delivered = $false
                    for ($i = 0; $i -lt 18; $i++) {
                        Start-Sleep -Seconds 5
                        $logs = (docker logs --since 5m robot-alert-logger 2>&1) | Out-String
                        if ($logs -match "CISyntheticAlert") { $delivered = $true; break }
                    }

                    # Resolve it either way so the synthetic condition is never left active.
                    Send-CIAlert (Get-Date).ToUniversalTime()

                    if (-not $delivered) {
                        Write-Error "Synthetic alert was accepted by Alertmanager but never reached the receiver - the notification path is broken"
                        exit 1
                    }
                    Write-Host "  [pass] synthetic alert delivered end to end after $($i*5)s, then resolved"
                '''

                // Traceability manifest: ties the build number to the commit, image ID,
                // test count and security posture it was released with, which otherwise
                // live in four separate systems.
                script {
                    // Assembled as text rather than with writeJSON/readJSON, which need the
                    // Pipeline Utility Steps plugin. trivy-summary.json is spliced in
                    // verbatim - it is already valid JSON.
                    def testCount = fileExists('testresults/test-count.txt')
                        ? readFile('testresults/test-count.txt').trim() : 'unknown'
                    def security = fileExists('security/trivy-summary.json')
                        ? readFile('security/trivy-summary.json').trim() : '{}'

                    def manifest = """{
    "build": {
        "number": "${env.BUILD_NUMBER}",
        "job": "${env.JOB_NAME}",
        "startedAtEpochMs": ${currentBuild.startTimeInMillis}
    },
    "source": {
        "repository": "https://github.com/uiqvb/RobotControllerApi",
        "branch": "${env.SOURCE_BRANCH ?: 'unknown'}",
        "commit": "${env.GIT_COMMIT ?: 'unknown'}"
    },
    "artefact": {
        "image": "${IMAGE_NAME}:${IMAGE_TAG}",
        "imageId": "${env.IMAGE_ID}",
        "releaseTag": "v${IMAGE_TAG}",
        "imageIdVerifiedAt": ["test stack", "staging", "production"]
    },
    "verification": {
        "testsExecuted": "${testCount}",
        "qualityGate": "passed",
        "security": ${security}
    },
    "deployed": {
        "staging": "http://localhost:${STAGING_PORT}",
        "production": "http://localhost:${PROD_PORT}"
    }
}"""

                    writeFile file: 'build-manifest.json', text: manifest
                    archiveArtifacts artifacts: 'build-manifest.json', allowEmptyArchive: false
                    echo "--- build manifest ---\n${manifest}"
                }

                echo "Grafana: http://localhost:3000   Prometheus: http://localhost:9090   Alertmanager: http://localhost:9093"
            }
            post {
                always  { echo 'Monitoring stage complete' }
                success { echo 'Monitoring live, targets healthy, alert rules loaded, notification path verified' }
                failure { echo 'Monitoring FAILED - the release is live but unobserved. Treat as an incident.' }
            }
        }
    }

    post {
        success {
            echo """
            ================================================================
             PIPELINE SUCCESS - build #${BUILD_NUMBER}
             Image released:  ${IMAGE_NAME}:${IMAGE_TAG}
             Staging:         http://localhost:${STAGING_PORT}/health
             Production:      http://localhost:${PROD_PORT}/health
             Grafana:         http://localhost:3000
             Git tag:         v${IMAGE_TAG}
             Deployable image built in stage 1 and promoted unchanged to production.
            ================================================================
            """
        }
        failure {
            // The pipeline does not track how far promotion got, so this does not
            // claim anything about staging or production state - a failure in stage 7
            // leaves a live release in place.
            echo "PIPELINE FAILED at build #${BUILD_NUMBER}. Check the failing stage above for the reason and the current state of each environment."
        }
        always {
            // Dangling images only - `prune` without -a does not remove tagged images,
            // so the previous known-good release tags used for rollback survive.
            // Consequence: myapp:* tags accumulate and are never reclaimed here, and a
            // manual `docker image prune -a` or `docker system prune -a` would delete the
            // rollback target recorded in DEPLOY_STATE. There is no registry to pull it
            // back from, so rollback is best-effort rather than guaranteed.
            bat 'docker image prune -f --filter "until=168h" || exit /b 0'
        }
    }
}
