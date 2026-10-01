# Security findings — RobotControllerApi

**Policy version:** 2 — 1 October 2026. Supersedes the 20 September version, which gated only on
fixable CRITICAL findings.
**Artefact scanned:** the image built in stage 1 of each build, never a rebuild.
**Scanner:** Aqua Trivy `0.58.1` (pinned), scanners `vuln`, `secret` and `config`.
**Pipeline stage:** Stage 4 — Security.
**Owner of every decision below:** Manit Khera.

Every HIGH or CRITICAL finding in this document has an explicit decision: **remediated**, or
**excepted with a compensating control and a review date**. Nothing is accepted indefinitely.

---

## 1. What changed in version 2, and why

The 7.3HD feedback named three gaps in version 1:

1. The gate covered only CRITICAL findings with a fix available. HIGH findings had no decision.
2. Configuration issues were scanned but never enforced.
3. `--ignore-unfixed` let unfixable findings pass with no review schedule. **An unavailable fix
   does not remove the risk.**

Version 2 answers each one, and the first answer was to remove the findings rather than write
better reasons for keeping them:

| | Version 1 (Debian 12 base) | Version 2 (chiselled Ubuntu base) |
|---|---:|---:|
| CRITICAL | 4 | **0** |
| HIGH | 63 | **2** (one CVE, reported against two packages) |
| MEDIUM | 121 | 9 |
| LOW | 121 | 11 |
| **Total** | **310** | **22** |
| Secrets | 0 | 0 |
| Application (NuGet) findings | 0 | 0 |

## 2. Remediation: rebase onto a chiselled runtime image (M1, done)

Version 1 recorded that **61 of the 63 HIGH findings, and all 4 CRITICAL ones, sat in Debian
packages the application never uses** — `perl-base`, `util-linux`, `ncurses`, `zlib` tooling — plus
10 in the `curl` our own Dockerfile added for the health check. It also recorded the fix and
deferred it: rebase onto `mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled-extra`.

That is now done. A chiselled image contains the .NET runtime and its native libraries and nothing
else: **no shell, no package manager, no curl, no perl, no util-linux.** Removing the packages
removes the findings — a stronger result than any exception could give. `-extra` adds ICU and
tzdata, so culture and time-zone behaviour match the old image.

The one cost was the health check. The old `HEALTHCHECK` shelled out to `curl`; a chiselled image
has neither a shell nor curl. The app now probes itself: `dotnet RobotControllerApi.dll
--healthcheck` calls `/health` on the local listener and exits 0 or 1 (top of `Program.cs`), run
in exec form. Verified before commit: the container reports `healthy`, runs as UID 1654 (the
image's built-in non-root user), and the integration suite passes 7/7 against it.

The build now also runs `docker build --pull`, so the newest patched base is fetched every build
and upstream OS fixes arrive without a code change.

## 3. The gate policy

Enforced in stage 4 of the `Jenkinsfile`, in this order:

| # | Rule | How it is enforced |
|---|---|---|
| 1 | **Any HIGH or CRITICAL vulnerability or secret fails the build, fixable or not.** | `trivy image --severity HIGH,CRITICAL --exit-code 1`. No `--ignore-unfixed`. |
| 2 | The only way past rule 1 is an explicit exception in `.trivyignore.yaml`. | `--ignorefile /project/.trivyignore.yaml` |
| 3 | Every exception has a statement (decision, reason, compensating control, owner) and a **review date no more than 30 days out**. | A PowerShell check validates the file *before* Trivy reads it and fails the stage on a missing statement, a missing date, or a date beyond `MAX_EXCEPTION_DAYS`. |
| 4 | **On the review date the exception expires**, the finding counts again, and the build fails until someone re-decides. | Trivy's `expired_at`. The review schedule is enforced by the pipeline, not by memory. |
| 5 | Any HIGH or CRITICAL **Dockerfile misconfiguration** fails the build, under the same exception rules. | `trivy config --severity HIGH,CRITICAL --exit-code 1`. Currently 0 findings. `--skip-check-update` pins the checks to the Trivy version so the result cannot shift between builds. |
| 6 | MEDIUM and LOW are reported and archived on every build, not gated. | Full table and JSON archived per build; summary in the build manifest. |

**Why the gate can be this strict now.** Version 1 argued that a HIGH gate over 63 unfixable
findings would block every build until someone disabled it. That was true of that image. With the
packages gone the HIGH count is one CVE, so the stricter gate is enforceable and stays on.

**What the reports show.** The two report passes run *without* the exceptions file, so the
archived record always shows everything Trivy found, excepted or not. Only the gate reads
exceptions. The console summary splits HIGH/CRITICAL findings into "covered by an active
exception" and "not covered — these gate".

## 4. Exceptions register

One exception is active. Its source of truth is `.trivyignore.yaml`; this is the reasoning.

### 4.1 CVE-2026-84782 — `openssl` / `libssl3t64` 3.0.13-0ubuntu3.15

| | |
|---|---|
| **Severity** | HIGH (reported once per package, so it appears as 2 findings) |
| **What it is** | Information disclosure through DTLS handshake retransmission |
| **Fix status** | Fixed in Ubuntu `3.0.13-0ubuntu3.16`. **Not yet in the Microsoft chiselled base image**, which has no package manager and so cannot be patched in place. |
| **Decision** | Excepted until **15 October 2026** |
| **Why** | Not reachable. DTLS is TLS over UDP. The API serves plain HTTP over TCP, bound to loopback, and opens no UDP sockets. |
| **Compensating control** | `docker build --pull` fetches the newest base every build. When Microsoft republishes the image with 3.16, the next build picks it up and the finding disappears with no code change. |
| **On review** | If the base has been republished, delete the exception. If not, re-check reachability and renew for at most 30 days with the reason stated again. |

## 5. MEDIUM and LOW findings

20 findings (9 MEDIUM, 11 LOW), all in the chiselled base's own libraries (`libc6`, `zlib1g`,
`libicu74` and similar). Some have a fix upstream; none can be applied in place. They are not
gated, by design: rule 6 above. They are reported on every build, they shrink automatically as
Microsoft republishes the base, and they are reviewed at the same time as the exceptions register.
If one is re-rated HIGH, the gate catches it on the next build.

## 6. Application-layer findings (not detected by Trivy)

Image scanning finds vulnerable *packages*. It cannot find vulnerable *logic*. These came from
manual review of the application's own code and are recorded here for completeness.

### 6.1 First-registration Admin bootstrap
**What it is.** `POST /api/auth/register` is `[AllowAnonymous]`. `AuthService.Register` applies a
bootstrap rule:

```csharp
// The very first user in an empty database becomes Admin.
// Every later anonymous registration becomes User.
var role = _users.GetAppUsers().Any() ? "User" : "Admin";
```

On a **freshly deployed environment with an empty database**, the first anonymous caller to reach
the endpoint is granted the `Admin` role. Because deployment and first use are separated in time,
an attacker who can reach the service before the legitimate operator registers can claim the
administrator account.

**Severity:** MEDIUM. Bounded by three factors: the window is only open on an empty database and
closes permanently after the first registration; the caller cannot *choose* a role, because
`RegisterRequest` exposes only `Email`, `DisplayName` and `Password` and the role is assigned
server-side; and every subsequent registration is forced to `User`. It is a narrow first-boot race,
not a privilege-escalation primitive.

**Whether addressed.** **Documented, not fixed — deliberately.** Both deployed environments bind to
`localhost` on a single developer machine and are not routable from any other host, so the window
is not externally reachable in this deployment. Two remediation options were considered:

- Seed the administrator account at deploy time from a Jenkins credential, and remove the bootstrap
  branch entirely. This is the correct production fix.
- Gate `register` behind a configuration flag that is off in `.env.prod`, so production accepts no
  anonymous registration at all.

The second is a two-line change, but it would break integration test I7 and the test fixture, which
register users to obtain credentials — so it needs test changes alongside it. Recorded as
remediation item M2 in §8.

### 6.2 Anonymous self-registration enabled in production
**What it is.** Beyond the bootstrap case, any unauthenticated caller reaching the production
listener can create a `User` account.
**Severity:** LOW in this deployment.
**Whether addressed.** Accepted. `User` is the least-privileged role, accounts are created with no
resource grants, and the production listener is bound to localhost. For an internet-facing
deployment this would need the same configuration flag as 7.1.

### 6.3 Basic authentication over plain HTTP
**What it is.** The API uses HTTP Basic authentication on every endpoint except `/health` and
`/api/auth/*`. Basic auth transmits credentials base64-encoded, which is encoding, not encryption,
and both deployed environments serve plain HTTP.
**Severity:** MEDIUM in principle; LOW in this deployment.
**Whether addressed.** **Mitigated by deployment topology, documented.** Traffic never leaves the
host: staging on `localhost:8090` and production on `localhost:8091` are published only to the
loopback interface, so there is no network segment on which credentials could be intercepted. Any
deployment beyond this machine must terminate TLS in front of the application — a reverse proxy
handling HTTPS is the standard placement, since the container itself holds no certificate and
should not.

### 6.4 Positive findings worth recording
Not everything found by review was a problem, and the controls that *are* present are part of the
security posture:

- The container runs as **non-root** (UID 1654, the chiselled image's built-in `app` user) in an
  image with **no shell and no package manager**, so even arbitrary code execution has no tools to work with.
- **No secrets in the image** — confirmed independently by Trivy's secret scanner (0 findings).
- Passwords are stored as hashes via `MultiPasswordHashService`, with the algorithm recorded per
  credential so hashes can be migrated without a mass reset.
- **Account lockout is implemented** — 5 failed logins trigger a 15-minute lockout, which
  meaningfully limits online password guessing.
- Authorisation **fails closed**: integration test I7 asserts that an unauthenticated
  `GET /api/maps` returns **401**, and it passes. The fallback policy denies by default rather than
  allowing anything not explicitly protected.

---

## 7. Risk register

| # | Finding | Severity | Decision | Review |
|---|---|---|---|---|
| R1 | 4 CRITICAL + 61 HIGH in unused Debian base packages | CRITICAL/HIGH | **Remediated** — packages removed by the chiselled rebase (§2) | Closed |
| R2 | 10 HIGH in `curl` / `libcurl4`, added by our Dockerfile | HIGH | **Remediated** — curl removed; the app probes its own health (§2) | Closed |
| R3 | CVE-2026-84782, `openssl`, DTLS | HIGH | **Excepted** — not reachable; fix arrives through `--pull` (§4.1) | **15 Oct 2026**, enforced by expiry |
| R4 | 9 MEDIUM + 11 LOW in chiselled base libraries | MEDIUM/LOW | Reported every build, not gated (§5) | With R3 |
| R5 | First-registration Admin bootstrap race | MEDIUM | Documented — window closed by localhost-only binding (§6.1) | Before any non-localhost deployment |
| R6 | Basic auth over plain HTTP | MEDIUM | Mitigated — loopback only (§6.3) | Before any non-localhost deployment |

## 8. Remediation roadmap

| # | Action | Status |
|---|---|---|
| M1 | Rebase production onto `aspnet:8.0-noble-chiseled-extra`; replace the curl HEALTHCHECK | **Done, 1 Oct 2026** |
| M2 | Seed the admin account at deploy time from a Jenkins credential; delete the bootstrap branch | Open — needs integration-fixture changes |
| M3 | Terminate TLS at a reverse proxy in front of both environments | Open — only meaningful beyond localhost |
| M4 | `trivy config` on the Dockerfile | **Done** — and now gating (rule 5) |
| M5 | Tighten the gate from CRITICAL to HIGH once M1 lands | **Done, 1 Oct 2026** — and without `--ignore-unfixed` |

## 9. Evidence

Attached to every build in Jenkins:

- `security/trivy-full-report.txt` — every finding, all severities, exceptions not applied
- `security/trivy-report.json` — the machine-readable record
- `security/trivy-config-report.txt` — Dockerfile misconfiguration scan
- `security/trivy-summary.json` — counts, excepted vs gating; also spliced into `build-manifest.json`
- `.trivyignore.yaml` — the exceptions in force for that build
- Console: the SECURITY EXCEPTIONS table (each exception, review date, days left) and the
  SECURITY SUMMARY, then the two gate results
