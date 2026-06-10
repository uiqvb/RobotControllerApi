# Frontend integration & MOVE_BACKWARD enablement

## 1. Where to put `index.html`

Replace the existing file at:

```
RobotControllerApi/wwwroot/index.html
```

Nothing else changes. `Program.cs` already calls `app.UseDefaultFiles()` and `app.UseStaticFiles()`, which serves `wwwroot/index.html` at the root URL of the API (e.g. http://localhost:5232/).

- No build step.
- No new NuGet packages.
- No CSS or JS files outside the HTML (the dashboard is single-file).
- No backend code changes required for the dashboard to function.
- Default credentials (`admin@test.com` / `<REDACTED-PASSWORD>`) and `deviceId=1` are pre-filled; users save once and they persist in `localStorage`.

After dropping the file in:

```bash
dotnet run --project RobotControllerApi
```

Then open http://localhost:5232/

## 2. Endpoints the dashboard uses

All Basic-Auth, except the GETs on `job-history` and `rollback-requests` whose controllers are not decorated with `[Authorize]` and accept anonymous calls in this build.

Reads (polled every 2 s):
- `GET /api/devices/{deviceId}/telemetry/summary` &nbsp;→ live mode, rain, buffer, line sensors, sun mode, RSSI, isStale flag, latest event
- `GET /api/devices/{deviceId}/telemetry?limit=50` &nbsp;→ raw telemetry table + event timeline
- `GET /api/devices/{deviceId}/jobs` &nbsp;→ recent-jobs table
- `GET /api/devices/{deviceId}/job-history` &nbsp;→ history table + LOCAL_SAFETY_OVERRIDE detection + proof checks
- `GET /api/devices/{deviceId}/rollback-requests` &nbsp;→ backend rollback list

Reads (polled every 60 s):
- `GET /api/devices/{deviceId}/capabilities` &nbsp;→ to detect MOVE_BACKWARD availability
- `GET /api/command-catalogue` &nbsp;→ to map IDs to names when a job lists an ID the UI doesn't know

Writes:
- `POST /api/devices/{deviceId}/jobs` body `{ commandCatalogueId, payloadJson:"{}", providerType:"Api", isRollback:false }` &nbsp;→ every queued command (racecar + grid).
- `POST /api/jobs/{jobId}/rollback` body `{ deviceId, requestedByProvider:"Api", jobHistoryIds:[], allowDuplicate:false }` &nbsp;→ used when the user clicks the "Rollback" button next to a Completed job. The backend `RollbackService.GenerateRollbackForJob` auto-populates `jobHistoryIds` from the job, so the empty list is intentional.

No legacy or unused endpoints are called.

## 3. MOVE_BACKWARD (ID 6): what's already done, what's missing

I checked the seed SQL at `Infrastructure/DataAccess/Database/Final_project_reference_data.sql`. Both rows already exist:

```sql
-- commandcatalogue row id=6
INSERT INTO public.commandcatalogue (id, name, description, isactive, ...)
VALUES (6, 'MOVE_BACKWARD', 'Move backward using adapter-specific behavior.', true, ...);

-- devicecapability row id=6 binds deviceId=1 to commandcatalogueid=6
INSERT INTO public.devicecapability (id, deviceid, commandcatalogueid, requiresmap, ...)
VALUES (6, 1, 6, false, 'Move backward using local adapter semantics.', true, ...);
```

So no backend schema, DTO, controller, or seed change is needed. `JobService.CreateJob` and `WorkDispatchService.ClaimNext` will accept and dispatch ID 6 jobs without modification.

The only gap is in the Arduino firmware at `SmartClotheslineNanoApiAdapter.ino`. The handler block for `MOVE_BACKWARD` already exists at lines 1224–1228 and calls the existing `manualPulseBackward()` function from line 697. What's missing is the case in `mapCommandCatalogueIdToName()` at lines 1098–1121 — it doesn't yet translate `commandCatalogueId == 6` to the string `"MOVE_BACKWARD"`, so the incoming Job is currently falling through to `UNSUPPORTED_COMMAND`.

### Exact `.ino` change (one line)

Open `SmartClotheslineNanoApiAdapter.ino` and find:

```cpp
String mapCommandCatalogueIdToName(int commandCatalogueId) {
  // Current local dev command IDs:
  // 1 AUTO
  // 2 STOP
  // 3 ROTATE_LEFT
  // 4 ROTATE_RIGHT
  // 5 MOVE_FORWARD
  // 7 RETURN_TO_SAFE_ZONE
  // 8 SET_MODE_FAST
  // 9 SET_MODE_SLOW
  // 10 REPORT
  switch (commandCatalogueId) {
    case 1: return "AUTO";
    case 2: return "STOP";
    case 3: return "ROTATE_LEFT";
    case 4: return "ROTATE_RIGHT";
    case 5: return "MOVE_FORWARD";
    case 7: return "RETURN_TO_SAFE_ZONE";
    case 8: return "SET_MODE_FAST";
    case 9: return "SET_MODE_SLOW";
    case 10: return "REPORT";
    default: return "";
  }
}
```

Add `case 6` and the comment:

```cpp
String mapCommandCatalogueIdToName(int commandCatalogueId) {
  // Current local dev command IDs:
  // 1 AUTO
  // 2 STOP
  // 3 ROTATE_LEFT
  // 4 ROTATE_RIGHT
  // 5 MOVE_FORWARD
  // 6 MOVE_BACKWARD
  // 7 RETURN_TO_SAFE_ZONE
  // 8 SET_MODE_FAST
  // 9 SET_MODE_SLOW
  // 10 REPORT
  switch (commandCatalogueId) {
    case 1: return "AUTO";
    case 2: return "STOP";
    case 3: return "ROTATE_LEFT";
    case 4: return "ROTATE_RIGHT";
    case 5: return "MOVE_FORWARD";
    case 6: return "MOVE_BACKWARD";
    case 7: return "RETURN_TO_SAFE_ZONE";
    case 8: return "SET_MODE_FAST";
    case 9: return "SET_MODE_SLOW";
    case 10: return "REPORT";
    default: return "";
  }
}
```

Re-flash the Nano. That's it.

### How to verify

After flashing:

1. In the dashboard, the orange "MOVE_BACKWARD may be unavailable on the Nano firmware" banner under the D-pad disappears once `GET /api/devices/1/capabilities` and `GET /api/command-catalogue` both confirm ID 6. (The banner is driven by *frontend* detection only — it doesn't mean the API call to queue ID 6 will fail.)
2. With rain not detected and mode not `FOLLOWING_LINE`/`SAFE_ZONE_REACHED`, press the Back button (or `S` / `↓`). A `MOVE_BACKWARD` job is queued, the Nano claims it, calls `manualPulseBackward()`, and reports `Completed`. The proof check "MOVE_FORWARD safety rejection" is unaffected.
3. Grid Back now works end-to-end and pose updates after job `Completed`.

## 4. Safety, override, and the `LOCAL_SAFETY_OVERRIDE` failure code

The dashboard mirrors the Nano's local safety logic so users see consistent state:

- **Locked**: when `rainDetected=true` or `mode ∈ {FOLLOWING_LINE, SAFE_ZONE_REACHED}`, the manual D-pad and Grid Forward/Back are disabled. A yellow banner explains why.
- **Dev Override**: a header toggle marked `Dev Override` re-enables those buttons. It also surfaces a clear warning that **the local Nano will still reject motion with `LOCAL_SAFETY_OVERRIDE`** — the override is UI-only.
- The override state is persisted in `localStorage` so it survives reloads (useful during demo recording).
- The proof checklist watches `job-history` for any row whose `commandCatalogueId == 5` and `failureCode == 'LOCAL_SAFETY_OVERRIDE'`, and turns the corresponding bullet green when it sees one.

## 5. Grid Mode behaviour summary

- 8×8 grid drawn as SVG. Robot is a triangle with rotation by heading.
- Grid commands queue **one at a time**; a second click is blocked until the in-flight job completes/fails.
- Pose state `{x, y, heading}` and the rollback stack live in `localStorage`. Reload preserves both.
- A Grid Forward queues `MOVE_FORWARD` (ID 5). Grid Back queues `MOVE_BACKWARD` (ID 6). Turn Left/Right queue `ROTATE_LEFT`/`ROTATE_RIGHT` (IDs 3, 4).
- Pose is only updated **after** the job reports `Completed`. If the job `Failed` or `Cancelled`, pose stays put and a toast explains why.
- "Undo Last Grid Move" pops the most recent non-undone stack entry, queues its inverse command, and on completion restores the original `oldPose`. The original stack entry is marked `UNDONE` instead of pushing a new entry. Clicking Undo repeatedly keeps walking back through the stack.
- Movement commands fired from the Racecar D-pad (or WASD/arrow keys while in Racecar mode) flip a flag in `localStorage` that triggers a banner in Grid mode: *"Manual driving may have invalidated grid pose. Reset or re-Set pose if needed."*

## 6. Proof / test panel

The panel is purely an observer over the polled data. Each item is computed live:

| Bullet | Computed from |
| --- | --- |
| `SET_MODE_SLOW completed` / `SET_MODE_FAST completed` / `STOP completed` / `AUTO completed` | any `Completed` job with the matching `commandCatalogueId` |
| `Rain override end-to-end` | telemetry window contains both `rainDetected=true` and an entry with `mode='SAFE_ZONE_REACHED'` (or an event of that type) |
| `MOVE_FORWARD safety rejection` | `job-history` contains a row with `commandCatalogueId=5` and `failureCode='LOCAL_SAFETY_OVERRIDE'` |
| `Offline fallback (Nano buffer)` | any telemetry row in the window has `bufferedEventCount > 0` |

For the demo: queue SET_MODE_SLOW, SET_MODE_FAST, STOP, AUTO. Block the rain sensor or set `rainDetected=true` from the Nano side. Try MOVE_FORWARD while it's raining. Pull the Wi-Fi briefly and watch `bufferedEventCount` rise on reconnect. By the end of the demo, all six bullets light up green.

## 7. Connection-state pill

The header pill reads `GET /api/devices/{deviceId}/telemetry/summary` and uses:

- `green`: the summary returns and `isStale=false`.
- `orange`: the summary returns and `isStale=true` (API up, device hasn't published telemetry recently).
- `red`: the request 4xx/5xx-d or the fetch threw. The pill text shows which: `Auth failed (401)`, `Not found. Is deviceId N valid?`, `API offline / unreachable.`, etc.
