# Security findings — RobotControllerApi

**Artefact scanned:** `myapp:1.0.6` (image ID `sha256:5fd41f052444…`) — the exact image built in
stage 1 of build #6 and released to production, not a rebuild.
**Scanner:** Aqua Trivy (`aquasec/trivy:latest`, schema v2), scanners `vuln` and `secret`.
**Scan date:** 20 September 2026.
**Pipeline stage:** Stage 4 — Security.

Every finding below is recorded with three things, as the task brief requires: **what it is**, its
**severity**, and **whether and how it was addressed** — fixed, justified, or mitigated.

---

## 1. How scanning is performed

The Security stage runs Trivy three times against the image, deliberately:

| Pass | Command shape | Exit behaviour | Purpose |
|---|---|---|---|
| 1 | `--severity LOW,MEDIUM,HIGH,CRITICAL --format table` | never fails | Human-readable report, archived as a build artefact |
| 2 | `--scanners vuln,secret --format json` | never fails | Machine-readable record for this document |
| 3 | `--severity CRITICAL --exit-code 1 --ignore-unfixed` | **fails the build** | The enforcement gate |

Separating *reporting* from *enforcement* is the key design decision. If the only Trivy invocation
were the blocking one, a failed build would produce no evidence of *why* it failed, and a passing
build would produce no evidence at all. Reporting first means there is always a complete record
attached to every build, pass or fail.

This stage is kept conceptually distinct from stage 3 (Code Quality). Code Quality asks *"is this
code healthy for the developers who maintain it"* — duplication, complexity, smells. Security asks
*"what can an attacker do to this artefact and its users"*. They are different questions with
different audiences and different remediation owners, and conflating them is a common mistake.

---

## 2. Results summary

Scan of `myapp:1.0.6`, all severities, both scanners:

| Target | Class | CRITICAL | HIGH | MEDIUM | LOW | UNKNOWN | Total |
|---|---|---:|---:|---:|---:|---:|---:|
| `myapp:1.0.6` (debian 12.15), 107 OS packages | os-pkgs | 4 | 63 | 121 | 121 | 1 | **310** |
| `app/RobotControllerApi.deps.json` | lang-pkgs (dotnet-core) | 0 | 0 | 0 | 0 | 0 | **0** |
| `Microsoft.AspNetCore.App/8.0.31` | lang-pkgs (dotnet-core) | 0 | 0 | 0 | 0 | 0 | **0** |
| `Microsoft.NETCore.App/8.0.31` | lang-pkgs (dotnet-core) | 0 | 0 | 0 | 0 | 0 | **0** |
| Secret scan (whole filesystem) | secret | — | — | — | — | — | **0** |

Two numbers matter more than the total:

**Zero findings in the application's own dependency tree.** Every one of the 310 findings is in a
Debian 12 base-image package inherited from `mcr.microsoft.com/dotnet/aspnet:8.0`. Not one comes
from a NuGet package this project chose, or from ASP.NET Core itself. The dependency hygiene of the
application is clean.

**Zero of 310 findings have a fixed version available.** This is the single most important fact in
this document and it is what makes the gate policy defensible — see §3.

**Zero secrets detected.** No credentials, tokens, connection strings or private keys are baked
into the image. This is by construction, not luck: the Dockerfile sets only non-sensitive
configuration (`ASPNETCORE_HTTP_PORTS`, telemetry opt-outs), and every environment-specific value
arrives at `docker run` time from `.env.staging` / `.env.prod`, which are gitignored and stored as
Jenkins *Secret file* credentials. They are written into the workspace during the Deploy and
Release stages and deleted in those stages' `post { always }` blocks.

---

## 3. The enforcement gate, and why `--ignore-unfixed`

```
trivy image --scanners vuln --severity CRITICAL --exit-code 1 --ignore-unfixed myapp:1.0.6
```

The gate fails the build on any **CRITICAL** vulnerability **for which a fix exists**. It currently
passes. That deserves an explicit justification rather than being quietly relied upon.

**Why CRITICAL and not HIGH.** A gate that fires on every build teaches the team to bypass it. With
63 HIGH findings, none of them fixable, a HIGH-level gate would block every single build
permanently, and the only way to ship would be to disable the gate — which is strictly worse than
having a narrower one that is genuinely respected. The HIGH findings are not ignored; they are
reported, archived, and reviewed in this document.

**Why `--ignore-unfixed`.** Without it the gate fails the build on the four CRITICALs in §4 — and
there is no action the build could take to pass, because Debian has published no patched package
for any of them. A gate that cannot be satisfied by any legitimate change is not a control; it is
an outage. `--ignore-unfixed` narrows enforcement to *actionable* findings: the moment Debian ships
a fix for any of these, the flag stops suppressing it and the next build fails until the base image
is rebased. The flag defers the finding, it does not dismiss it.

**What the gate would actually catch.** A newly introduced NuGet package with a known critical CVE;
a base image that has drifted behind an available security update; a critical vulnerability
disclosed and patched between one build and the next. All three are real, common, and actionable —
and all three are exactly what an image scanner is worth having for.

---

## 4. CRITICAL findings — all four, individually

All four are unfixed upstream. Disposition for each is **mitigated and accepted**, with reasoning.

### 4.1 CVE-2023-45853 — `zlib1g` 1:1.2.13.dfsg-1
**Severity:** CRITICAL. **Upstream status:** `will_not_fix`.
**What it is:** Integer overflow leading to a heap buffer overflow in minizip's
`zipOpenNewFileInZip4_6`, reachable when a caller passes an attacker-controlled filename longer
than 64 KB into the zip-writing helper.
**Whether addressed:** Mitigated by non-reachability, accepted. The vulnerable function lives in
minizip, a *contributed utility* bundled in the zlib source tree, not in the zlib library API. The
application never writes zip archives and never invokes minizip. Debian has classified it
`will_not_fix` on the same reasoning — the affected code is not compiled into the shipped
`zlib1g` library. There is no patched package to install.
**Residual risk:** Negligible. No code path in the container reaches the vulnerable function.

### 4.2 CVE-2026-13221 — `perl-base` 5.36.0-7+deb12u3
**Severity:** CRITICAL. **Upstream status:** `affected`, no fix published.
**What it is:** Incorrect regular-expression processing when compiling very large regular
expressions, leading to memory corruption.
**Whether addressed:** Mitigated by non-reachability, accepted.
**Residual risk:** Negligible — see the shared reasoning in §4.5.

### 4.3 CVE-2026-42496 — `perl-base` 5.36.0-7+deb12u3
**Severity:** CRITICAL. **Upstream status:** `fix_deferred`.
**What it is:** Path traversal in `Archive::Tar` via crafted symlinks inside an archive, allowing a
malicious tarball to write outside the extraction directory.
**Whether addressed:** Mitigated by non-reachability, accepted. The application extracts no
archives and executes no Perl.
**Residual risk:** Negligible.

### 4.4 CVE-2026-8376 — `perl-base` 5.36.0-7+deb12u3
**Severity:** CRITICAL. **Upstream status:** `affected`, no fix published.
**What it is:** Heap buffer overflow when compiling regular expressions on 32-bit builds.
**Whether addressed:** Mitigated by non-reachability *and* by architecture, accepted. The container
runs on x86-64; the overflow is specific to 32-bit builds, so it is not exploitable on this image
even if Perl were reachable.
**Residual risk:** None on this platform.

### 4.5 Shared reasoning for the three `perl-base` findings

Exploiting any of these requires the attacker to cause the container to **execute Perl** with
attacker-influenced input. In this image that cannot happen through the application:

- The entrypoint is `dotnet RobotControllerApi.dll`. No shell, no script interpreter, no Perl.
- The application contains no `Process.Start`, no shell-out, and no code path that invokes an
  external binary.
- The container runs as the unprivileged user `appuser` (UID 5678, no home directory, no shell
  login), so even arbitrary local execution would be confined.
- `perl-base` is present only because it is an `Essential: yes` package in every Debian base image.
  It is a transitive consequence of the base-image choice, not a dependency this project introduced
  or can remove with `apt-get remove` (doing so would break the package manager).

The honest summary: these are real vulnerabilities in code that ships inside the image, they are
not reachable from the attack surface this application actually exposes, and no upstream fix
exists to apply. They are accepted, recorded here, and re-evaluated on every build.

---

## 5. HIGH findings — 63, grouped by package

None of the 63 has a fixed version available. Grouped by the package that carries them:

| Package | Findings | Origin | Reachable from the app? | Disposition |
|---|---:|---|---|---|
| `curl` | 5 | **Added by our Dockerfile** | Only via HEALTHCHECK — see §6 | Accepted, documented |
| `libcurl4` | 5 | **Added by our Dockerfile** | Only via HEALTHCHECK — see §6 | Accepted, documented |
| `perl-base` | 5 | Debian `Essential` | No | Accepted (§4.5) |
| `util-linux` | 5 | Debian base | No | Accepted |
| `util-linux-extra` | 5 | Debian base | No | Accepted |
| `bsdutils` | 5 | Debian base | No | Accepted |
| `mount` | 5 | Debian base | No — container cannot mount | Accepted |
| `libmount1` | 5 | Debian base | No | Accepted |
| `libblkid1` | 5 | Debian base | No | Accepted |
| `libuuid1` | 5 | Debian base | No | Accepted |
| `libsmartcols1` | 5 | Debian base | No | Accepted |
| `libldap-2.5-0` | 1 | Debian base | No — no LDAP auth configured | Accepted |
| `libsystemd0` | 1 | Debian base | No — no init system in container | Accepted |
| `libudev1` | 1 | Debian base | No — no device management | Accepted |
| `ncurses-base` | 1 | Debian base | No — no TTY | Accepted |
| `ncurses-bin` | 1 | Debian base | No — no TTY | Accepted |
| `libtinfo6` | 1 | Debian base | No — no TTY | Accepted |
| `gzip` | 1 | Debian base | No | Accepted |
| `libacl1` | 1 | Debian base | No | Accepted |

The pattern is consistent and worth stating plainly: **61 of the 63 HIGH findings are in packages
that exist in the image only because Debian ships them, and that the application never calls.** The
container has no shell session, no init system, no TTY, no device access and no mount capability.
The `util-linux` family alone accounts for 30 findings in tooling (`mount`, `blkid`, `lsblk`,
`fdisk`) that is inert inside an unprivileged application container.

The remaining two packages — `curl` and `libcurl4`, 10 findings between them — are different,
because we put them there.

---

## 6. The one finding we caused ourselves — `curl`

This is the most actionable item in this document, and the only one traceable to a decision in our
own `Dockerfile` rather than to the base image.

```dockerfile
# curl is here solely for HEALTHCHECK; the runtime image ships without one.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
```

**What it is.** One line adds `curl` and `libcurl4`, contributing **10 HIGH findings**
(CVE-2026-12064, CVE-2026-6276, CVE-2026-8286, CVE-2026-8458, CVE-2026-8927 — each reported against
both packages) — 16% of all HIGH findings in the image.

**Severity:** HIGH ×10, all unfixed in Debian 12.

**Why it is there.** The image declares:

```dockerfile
HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=5 \
    CMD curl -fsS "http://localhost:${ASPNETCORE_HTTP_PORTS}/health" || exit 1
```

That HEALTHCHECK is load-bearing, not decorative. The compose stacks use
`depends_on: condition: service_healthy` for ordering, and the pipeline's own deploy verification
reads container health. Removing `curl` without a replacement probe would silently disable
dependency ordering across all three environments.

**Whether addressed.** **Accepted with justification, with a costed remediation path recorded
below.** The reasoning:

1. All 10 findings are unfixed in Debian 12, so `apt-get upgrade` cannot resolve them.
2. `curl` is never invoked by the application. It is executed only by the Docker daemon's health
   probe, against `http://localhost:8080/health` — a fixed, hard-coded, non-attacker-controllable
   URL on the container's own loopback interface. The typical `curl` exploitation pattern, in which
   an attacker steers a request to a URL of their choosing, has no analogue here.
3. It runs as `appuser`, unprivileged.

**Remediation path, and why it was not taken tonight.** The clean fix is to rebase the production
stage onto a chiselled runtime image — `mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled` — which
ships without a package manager, without a shell, and without `curl`, `perl`, `util-linux` or
`ncurses`. That would remove the great majority of the 310 findings at a stroke, because it removes
the packages rather than patching them.

The trade-off is that a chiselled image **has no shell**, which means the `HEALTHCHECK` above stops
working, which means `depends_on: service_healthy` stops working in all three compose files. The
replacement is a small self-contained health-probe binary published alongside the app and invoked
in exec form. That is a genuine, worthwhile change — and it is a change to the container contract
that every environment depends on, so it belongs in its own change with its own verification run,
not bolted on beside an assessment deadline. It is recorded in §9 as the top remediation item.

Recording this honestly is the point. The finding is real, we caused it, we understand the fix, we
can state its cost, and we made a deliberate decision to defer it rather than discovering it later.

---

## 7. Application-layer findings (not detected by Trivy)

Image scanning finds vulnerable *packages*. It cannot find vulnerable *logic*. These came from
manual review of the application's own code and are recorded here for completeness.

### 7.1 First-registration Admin bootstrap
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
remediation item 2 in §9.

### 7.2 Anonymous self-registration enabled in production
**What it is.** Beyond the bootstrap case, any unauthenticated caller reaching the production
listener can create a `User` account.
**Severity:** LOW in this deployment.
**Whether addressed.** Accepted. `User` is the least-privileged role, accounts are created with no
resource grants, and the production listener is bound to localhost. For an internet-facing
deployment this would need the same configuration flag as 7.1.

### 7.3 Basic authentication over plain HTTP
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

### 7.4 Positive findings worth recording
Not everything found by review was a problem, and the controls that *are* present are part of the
security posture:

- The container runs as **non-root** `appuser` (UID 5678, system account, no home, no shell login).
- **No secrets in the image** — confirmed independently by Trivy's secret scanner (0 findings).
- Passwords are stored as hashes via `MultiPasswordHashService`, with the algorithm recorded per
  credential so hashes can be migrated without a mass reset.
- **Account lockout is implemented** — 5 failed logins trigger a 15-minute lockout, which
  meaningfully limits online password guessing.
- Authorisation **fails closed**: integration test I7 asserts that an unauthenticated
  `GET /api/maps` returns **401**, and it passes. The fallback policy denies by default rather than
  allowing anything not explicitly protected.

---

## 8. Accepted risk register

| # | Finding | Severity | Disposition | Re-evaluate when |
|---|---|---|---|---|
| R1 | 4 CRITICAL CVEs in `zlib1g` / `perl-base`, all unfixed | CRITICAL | Accepted — not reachable; no patch exists | Debian publishes a fix; the gate will then fail the build automatically |
| R2 | 53 HIGH CVEs in unreachable Debian base packages | HIGH | Accepted — not reachable; no patch exists | Base image rebase (see M1) |
| R3 | 10 HIGH CVEs in `curl` / `libcurl4`, introduced by our Dockerfile | HIGH | Accepted — invoked only by the health probe against a fixed local URL | Immediately on completing M1 |
| R4 | 121 MEDIUM + 121 LOW in base packages | MEDIUM/LOW | Accepted — reported and archived, below the enforcement threshold | Base image rebase |
| R5 | First-registration Admin bootstrap race | MEDIUM | Documented — window closed by localhost-only binding | Before any non-localhost deployment |
| R6 | Basic auth over plain HTTP | MEDIUM | Mitigated — loopback-only, no interceptable segment | Before any non-localhost deployment |

**Standing rule:** none of these acceptances is permanent. Every one is re-tested on every pipeline
run, because the Security stage scans the freshly built image rather than consulting a stored
allow-list. The acceptance is of a *current, evidenced* state, not a suppression.

---

## 9. Remediation roadmap

| # | Action | Removes | Effort | Blocked by |
|---|---|---|---|---|
| M1 | Rebase the production stage onto `aspnet:8.0-noble-chiseled` and replace the curl HEALTHCHECK with a self-contained probe binary | The large majority of all 310 findings, including all 4 CRITICALs and R3 | ~2 h + a full verification run | Needs its own change and pipeline run; the HEALTHCHECK is load-bearing for `depends_on` in all three compose files |
| M2 | Seed the admin account at deploy time from a Jenkins credential; delete the bootstrap branch | R5 | ~1 h | Integration test fixture registers users to obtain credentials |
| M3 | Terminate TLS at a reverse proxy in front of both environments | R6 | ~1 h | Only meaningful once deployed beyond localhost |
| M4 | Add `trivy config` to lint the Dockerfile and compose files for misconfiguration alongside the vulnerability scan | Nothing today; catches future misconfiguration | ~20 min | None — the cheapest remaining hardening |
| M5 | Tighten the gate from `CRITICAL` to `HIGH` once M1 lands and the HIGH count is near zero | — | ~5 min | M1 |

M1 and M5 are paired on purpose. The reason the gate is set at CRITICAL today is that 63 unfixable
HIGH findings make a HIGH gate unenforceable. Remove the packages carrying them and the constraint
disappears, at which point the gate can be tightened and *stay* tightened. That is the difference
between loosening a control to get a green build and re-earning the right to enforce a stricter
one.

---

## 10. Evidence

Attached to every build in Jenkins:

- `security/trivy-full-report.txt` — the complete human-readable table, all severities
- `security/trivy-report.json` — the machine-readable record this document was written from
- Console output of the blocking gate, showing its exit status

The scan runs against `myapp:${IMAGE_TAG}` — the image built in stage 1 — and not against a
rebuild. The artefact scanned here is byte-for-byte the artefact running in production.
