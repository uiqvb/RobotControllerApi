# Context — what this codebase is and what recently changed

Written 1 August 2026. Intended for a person or AI picking this up cold.

---

## 1. What the project is

A .NET 8 / PostgreSQL **control plane for cheap, dumb robots**. The thesis: keep the
intelligence in the cloud and none on the robot.

A robot (an Arduino Nano 33 IoT "robocar" for a smart clothesline) polls the backend over HTTP,
claims **one command at a time**, executes a short motor pulse, and reports the result. The
backend owns everything else — grid position, map bounds validation, workflow policy, and
rollback. The robot deliberately holds no plan of its own.

**Persistence:** three interchangeable implementations behind the same interfaces — ADO
(`Npgsql`, the default), EF Core, and an in-memory repository. Selected by
`Persistence:Provider` in config.

**Layout:** `BoundedContexts/<Area>/{Controllers,Services,Persistence,Dtos,Models}`, with
`Infrastructure/DataAccess/` holding the three persistence implementations.

---

## 2. The core domain concepts

| Concept | Meaning |
|---|---|
| **Job** | One command. Answers *"did this single command work?"* |
| **Workflow** | An ordered list of jobs **plus a failure policy**. Answers *"did the plan work?"* |
| **ExecutionMode** | `BestEffort` = skip a bad step and continue. `AllOrNothing` = dry-run the whole remaining sequence before moving, and cancel the rest if a step fails. |
| **CommandCatalogue** | The command vocabulary. Carries `InverseCommandName` and `RollbackKind`. |
| **RollbackKind** | `Exact` (grid moves — the inverse is trusted), `BestEffort` (timed moves — reversal drifts), `None`. |
| **Grid pose** | The robot's believed position on a map, held in `DeviceStatus`, only trusted after a `PLACE`. |

**Key grid commands and their inverses:** `MOVE`↔`STEP_BACK`, `LEFT`↔`RIGHT`. `STEP_BACK`
reverses *without turning around*, so facing stays correct through a rollback.

`PLACE` is the checkpoint — it means "I know exactly where I am". Rollback never walks past one.

---

## 3. What changed on 1 August 2026 — offline rollback (D2)

**The problem being solved:** when the robot loses its connection mid-task, it used to just
stop. The goal was for it to reverse its own steps unaided and walk back toward connectivity,
then resync with the backend on reconnect.

### 3.1 The design decision behind it

Rollback used to be addressed **by workflow id** (`GenerateRollbackForWorkflow`). That is wrong
in principle: undoing a *position* is inherently a **stack (LIFO)** operation, because grid
position is cumulative and does not reset at workflow boundaries. Nothing stopped you reversing
an older workflow that was no longer top-of-stack, which produces off-grid nonsense.

The insight: **workflow boundaries are an authoring concept; position is cumulative and ignores
them.** So there are two layers, kept separate:

- **Authoring/dispatch layer** — Jobs + Workflows. Grouping is meaningful.
- **Positional layer** — a flat movement stack. Grouping is meaningless.

The robot-local (offline) path is LIFO by construction, so the broken case is *unrepresentable*
rather than merely discouraged.

### 3.2 What was built

**`BoundedContexts/Shared/CompensationBuilder.cs`** *(new)*
The "what undoes this command" logic, extracted into one pure, data-access-free place. It was
previously private to `RollbackService` and therefore only reachable *after* a command had
executed and been historised. The offline path needs it *at dispatch time*, before the robot
moves. Both paths now call the same code — two copies would silently drift, making the dashboard
rollback behave differently from the robot's.

**`WorkItemClaimResponse`** *(extended)*
Now carries `InverseCommandName` + `InversePayloadJson`. This is the whole foundation: the robot
is told how to undo a command *at the moment it is given it*, so it can build a local undo stack.

Only `Exact`-kind commands get an inverse. `BestEffort` inverses are timed reversals that
accumulate dead-reckoning error, and an offline robot gets none of the server-side pre-flight
validation that would normally catch a bad step. **A null inverse is a normal answer, never an
error** — dispatch must not fail because a command happens to be irreversible.

**`POST /api/adapter/devices/{deviceId}/work-items/report-offline-rollback`** *(new)*
The robot reports what it executed while offline; the backend replays it through the same pose
rules and resyncs.

The whole report is **simulated before any of it is committed**. A half-applied rollback would
record a position the robot was never at — worse than admitting the position is unknown.

**Pose trust restoration.** Losing the connection is exactly what *drops* pose trust
(`ExpireStaleWork` → `InvalidatePose`), so reconciliation deliberately does **not** require
trusted pose to begin. Invalidation clears the flags but leaves the coordinates, and those
coordinates are the anchor the replay starts from. Without this, the demo's own trigger
condition would brick grid movement afterwards.

**`RolledBack` status, finally written.** The status existed in the schema and `DomainConstants`
and was *read* in six places, but **nothing in the codebase ever assigned it** — a rolled-back
workflow sat at `Completed` forever. Originals are now marked when the compensating work
*completes* (not when it is generated — a rollback that fails to execute has reversed nothing).

**`Infrastructure/StaleWorkExpiryService.cs`** *(new)*
Expiry previously ran only inside `claim-next`, so a stalled robot's queue could only be drained
by that same stalled robot polling again. Now swept on a timer
(`WorkDispatch:StaleExpirySweepSeconds`, default 30).

**Timestamp fix (`Program.cs`).** Every timestamp column is `timestamp without time zone` and
every value written is `DateTime.UtcNow`. Npgsql 6+ maps `Kind=Utc` to `timestamptz`, so
PostgreSQL converted it into the server's local zone on the way in — a UTC+10 machine stored
15:25 where 05:25 was meant. Lease expiry landed ten hours in the future and never fired, which
silently disabled the stale-work sweep above.
Fixed with `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)`.
⚠️ **Rows written before this fix hold local time; rows after hold UTC.** Old rows were not
migrated.

**No schema changes were required.** Every value written is already permitted by the existing
check constraints (verified against the live database, not just the `.sql` files).

### 3.3 The firmware side

Lives outside this repo, in a `Firmware/` folder one level up (gitignored — the sketches carry
WiFi and device credentials in plaintext).

`SmartClotheslineNano_OfflineRollback.ino` adds to the working minimal adapter:

- a **RAM-only undo stack** (24 entries). The Nano 33 IoT is SAMD21 and **has no EEPROM**, so a
  brownout wipes it. Accepted tradeoff — the buck converter is meant to remove the brownouts.
- a **watchdog** — fires after 7s without a backend reply. Fed centrally inside
  `sendHttpRequest`, so every request counts. A 4xx still counts as alive: the server answered.
- an **offline burst** — pops 5 inverses, executes them via the existing command path.
  "5 commands" counts turns, so a burst that is mostly turns barely moves. This was a deliberate
  choice over "5 movements".
- a **reconnect report**, cleared only on HTTP 200. Dropping it on a flaky reconnect would lose
  the robot's position permanently.
- **stack clearing** on `PLACE` (fresh known position) and on **live/WASD control** (free-hand
  driving moves the robot with no inverse recorded, so every stack entry becomes a lie).

---

## 4. Verified working

End-to-end on real hardware, 1 August 2026:

1. Robot executed `MOVE MOVE RIGHT MOVE MOVE LEFT` from `(1,1) North` → ended `(3,3) North`.
2. Undo stack reached 6.
3. Backend was killed. After ~7s the robot reversed **5 of 6** unaided, keeping one — the chunk
   rule working, not a bug.
4. Backend restarted. Robot reported its 5 offline steps; HTTP 200.
5. Backend replayed them and landed on **`(1,2) North`** — the pose predicted before the test —
   with trust restored.
6. The 5 reversed jobs flipped to `RolledBack`; the one never reversed stayed `Completed`.
7. Five audit rows written with `JobId` NULL, the marker for "executed offline, never dispatched".

---

## 4b. Voltage sag — fixed 1 August 2026

**The Nano used to reboot whenever the motors started, stopped, or reversed.** Root cause: it was
powered from the L298N's onboard 5V regulator — the *same* regulator the motor driver loads. Under
motor current that rail sagged below the regulator's dropout and the board browned out.

`docs/hardware-changes.md` had explicitly ruled out both a buck converter and separating the
supplies, opting for a bulk capacitor on the shared rail instead. **That call was wrong.** The fix
was to separate the supplies:

```
Battery + ──┬── L298N 12V
            └── Buck IN+
Battery − ──┬── L298N GND ──── − rail
            └── Buck IN−
Buck OUT+ ───── Nano VIN        (NOT the 5V or 3.3V pin — those bypass the regulator)
Buck OUT− ───── − rail
(the old L298N 5V → Nano wire was removed — that removal IS the fix)
```

Buck output set to **6.0V**. Not arbitrary: `VIN` needs ≥5V, and a step-down needs ~1.5V of
headroom above its output, so 6.0V keeps regulating until the pack falls to ~7.5V. Setting it
near the battery voltage (it shipped at 8.5V from a 9.1V pack) leaves no headroom and the
converter just passes the sag straight through.

**Verified, not assumed.** With no multimeter available, the firmware reports diagnostics with
every completed job (`bootId`, `uptimeMs`, `resetCause` from the SAMD21 reset-cause register).
A 10-step run with four forward↔reverse transitions produced **one single bootId across all ten
steps**, monotonic uptime, and `resetCause` never leaving `POWER_ON` — the brown-out detector
never fired. Before the fix the same load rebooted the board.

Note the diagnostics are read from the database, deliberately: attaching USB for serial would
power the Nano and mask the very brownouts under investigation.

---

## 5. Running it

```bash
dotnet run --urls "http://0.0.0.0:5232"
```

**The `--urls` flag matters.** The default binds to `localhost` only, which is invisible to the
robot. Bind `0.0.0.0` or the Nano cannot reach the backend at all.

- Swagger: `/swagger` (Development only). Authorize with Basic auth.
- Config lives in user secrets (`ConnectionStrings:DefaultConnection`,
  `DeviceCredential:HashKey`) — **not** in `appsettings.json`.
- The laptop's LAN IP must be written into the sketch, and **changes every time it rejoins the
  hotspot**. Check the Wi-Fi adapter specifically, not Ethernet/WSL/Tailscale.

**Device auth:** headers `X-Device-Credential-Id` / `X-Device-Credential-Secret`.
Secrets are HMAC-SHA256 keyed by `DeviceCredential:HashKey` and are **shown only once, at
creation**. Rotating that key invalidates every previously issued device secret — this happened
on 29 June 2026 and silently killed an older credential that was still sitting in a sketch.

---

## 6. Known issues and gaps

**Not built:**
- **D3 zone commands** (`MOVE_TO_SAFE_ZONE` / `MOVE_TO_DRY_ZONE`) — absent from the command
  catalogue *and* the firmware.
- **Frontend** — `wwwroot/` is a single `index.html`. No grid, pose display, rollback trigger,
  or device switcher.
- **Automated test suite** — the old Postman/Newman assets were deleted and never replaced.

**Accepted design limitations:**
- **The robot drives blind while offline.** No map or bounds validation — that is all
  server-side. The LIFO stack means it retraces steps it actually took, but nothing catches it
  if it started from a bad state.
- **The undo stack does not survive a reset** (RAM-only, see above).

**Housekeeping:**
- Old timestamp rows are in local time (see 3.2).
- Sketches contain plaintext WiFi and device credentials. Keep `Firmware/` out of the repo.
- An older app user exists whose password is lost; a newer admin account is in use instead.

---

## 7. Conventions

- **All open-day demo work happens on the `openday` branch.** `main` holds the tested, working
  backend and is the fallback. A `pre-commit` hook blocks commits on `main`.
- `CLAUDE.md`, `.claude/`, and `docs/` are gitignored so internal notes stay off the public repo.
- No AI attribution in commit messages or the README.
