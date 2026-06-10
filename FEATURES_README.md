# Smart Mobile Clothesline Robot — Feature Plan

This README lists the agreed feature set for the full system: embedded robot behaviour, backend/API behaviour, dashboard behaviour, notifications, fallback behaviour, and validation expectations.

---

## 1. System Goal

The system is a smart mobile clothesline robocar that can:

- Autonomously rotate toward the brightest sunlight when conditions are dry.
- Detect rain locally and move into shade by following a black line.
- Continue protecting clothes even if Wi-Fi/backend communication fails.
- Send telemetry to a backend.
- Accept remote commands from a frontend/dashboard.
- Support grid-style robot commands and workflows for the backend unit.
- Log jobs, workflows, telemetry, and execution history.
- Support rollback from actual successful execution history.
- Notify users or housemates when important events happen.

The robot should be treated as:

```text
Locally autonomous
+
remotely observable
+
remotely commandable
+
grid-capable for backend workflow/rollback requirements
```

---

## 2. Embedded Systems Features

### 2.1 Dry Weather / Sun Tracking

When there is no rain, the robocar should stay in drying/sun-tracking mode.

Required behaviours:

- Read left and right light sensors.
- Compare brightness levels.
- Rotate toward the brighter side.
- Avoid jitter from tiny brightness changes.
- Support FAST visual-test mode.
- Support SLOW realistic mode.
- Keep the robot/clothesline facing better sunlight.

### 2.2 Rain Detection and Protection

Rain detection is the highest-priority embedded behaviour.

Required behaviours:

- Detect rain using the rain sensor.
- Confirm rain using more than one reading to reduce false positives.
- Stop sun-tracking behaviour when rain is confirmed.
- Enter protection mode.
- Follow the black line into shade/safe area.
- Stop when the safe area or line end is reached.

### 2.3 Local Safety and Autonomy

The robot must continue working even if Wi-Fi or backend communication fails.

Required behaviours:

- Keep detecting rain locally.
- Keep moving to shade locally.
- Stop motors safely without backend dependency.
- Maintain basic autonomous behaviour offline.
- Treat backend/frontend commands as optional requests, not safety-critical control.

### 2.4 Telemetry / Status Reporting

The Nano should send telemetry/status to the backend.

Telemetry should include:

- Current mode.
- Raw rain value.
- `rainDetected` true/false.
- Left/right brightness readings.
- Line sensor states.
- Wi-Fi signal strength.
- FAST/SLOW sun mode.
- Optional communication state.
- Optional buffered event count.

Example telemetry payload:

```json
{
  "mode": "SUN_TRACKING",
  "rainRaw": 4018,
  "rainDetected": false,
  "luxLeft": 245.3,
  "luxRight": 318.7,
  "lineLeft": false,
  "lineCenter": false,
  "lineRight": false,
  "sunMode": "SLOW",
  "wifiRssi": -58
}
```

### 2.5 Wi-Fi Failure and Event Buffering

Important events must not be lost just because communication is down.

Required behaviours:

- Detect when Wi-Fi/backend is unavailable.
- Retry connection later.
- Continue reacting locally while offline.
- Buffer important rain/safety events locally.
- Send buffered events once connection returns.

Buffered events should include:

- Rain confirmed.
- Safe zone reached.
- Failed to reach safe zone.
- Wi-Fi lost.
- Wi-Fi recovered.

Minimum event example:

```json
{
  "eventType": "RAIN_CONFIRMED",
  "rainRaw": 2900,
  "mode": "FOLLOWING_LINE",
  "sequence": 42
}
```

### 2.6 Remote Command Handling

The robot should accept commands from the backend/frontend.

Supported embedded commands:

- `REPORT`
- `STOP`
- `AUTO`
- `SET_MODE_FAST`
- `SET_MODE_SLOW`
- `RETURN_TO_SAFE_ZONE`
- `ROTATE_LEFT`
- `ROTATE_RIGHT`
- `MOVE_FORWARD`
- `MOVE_BACKWARD`

Safety rule:

- `STOP` is always allowed.
- `REPORT` is always allowed.
- Unsafe manual movement should be rejected during rain/protection mode.
- Local safety always overrides remote control.

Example failure response:

```json
{
  "failureCode": "LOCAL_SAFETY_OVERRIDE",
  "failureMessage": "Rain override active. Manual movement rejected."
}
```

### 2.7 Non-Blocking Real-Time Control

The Nano must remain responsive.

Required behaviours:

- Avoid long blocking delays.
- Check rain sensor frequently.
- Update line following frequently.
- Read brightness sensors less frequently.
- Send telemetry periodically.
- Poll backend commands periodically.
- Use timer-based scheduling.
- Use a state machine for mode transitions.

Suggested priority:

```text
Highest:
  Rain detection
  Motor stop
  Line following

Medium:
  Backend command polling
  Telemetry upload

Lower:
  Light trend calculation
  Dashboard reporting
```

### 2.8 Debugging and Validation Support

The embedded system should expose enough information for testing.

Required behaviours:

- Print sensor values.
- Print mode changes.
- Count failed uploads.
- Show Wi-Fi reconnect attempts.
- Support FAST/SLOW demo modes.
- Track time from rain detection to movement start.
- Track successful/failed telemetry uploads.

---

## 3. Backend Unit Features

### 3.1 Robot-Neutral Backend

The backend should not know hardware details.

The backend should not contain:

- Arduino pins.
- GPIO logic.
- PWM values.
- Motor driver details.
- Wheel radius.
- Sensor calibration constants.
- Hardware-specific movement code.

The Nano/adapter translates backend commands into hardware behaviour.

### 3.2 Device and Capability Model

The backend should store devices and what they can do.

Required entities:

- `Device`
- `CommandCatalogue`
- `DeviceCapability`
- `DeviceStatus`
- `DeviceCredential`
- `DevicePermission`

The backend should validate:

- Device exists.
- Device is active.
- Command exists.
- Command is active.
- Device supports the command.
- Required map exists if command needs a map.
- Grid pose is trusted/aligned if command needs grid trust.

### 3.3 Grid Map Support

The backend unit requires grid-style robot behaviour.

Required behaviours:

- Store maps with rows, columns, and cell size.
- Assign devices to maps.
- Show whether a device is currently map-enabled.
- Support grid commands only when map requirements are satisfied.

Map features:

- `Rows`
- `Columns`
- `CellSizeCm`
- `IsActive`

### 3.4 Grid Pose / Alignment Status

The backend must track the robot's believed grid pose.

Device status should include:

- `GridX`
- `GridY`
- `Facing`
- `IsGridAligned`
- `IsGridPoseTrusted`
- `PoseMapId`
- Optional estimated physical position.

Rules:

- If `IsGridPoseTrusted = false`, grid movement should be blocked.
- If `IsGridAligned = false`, grid movement should be blocked.
- `PLACE` can restore grid trust.
- Free-run/manual movement may invalidate grid trust.

### 3.5 PLACE Command

`PLACE` sets the current physical position as the new grid source of truth.

Example:

```text
PLACE 0,0,North
```

Meaning:

```text
The robot's current physical position is now treated as grid cell 0,0 facing North.
```

Rules:

- `PLACE` does not physically drive the robot to that cell.
- `PLACE` sets/resets logical grid pose.
- After `PLACE`, grid commands may be allowed.
- `PLACE` acts as a rollback boundary.

### 3.6 Grid Movement Commands

The backend unit should support grid commands:

- `PLACE`
- `MOVE`
- `LEFT`
- `RIGHT`
- `STEP_BACK`
- `REPORT`

Adapter/Nano translation examples:

```text
MOVE      -> drive one cell distance
LEFT      -> rotate -90 degrees
RIGHT     -> rotate 90 degrees
STEP_BACK -> reverse one cell distance
REPORT    -> return status
```

These are different from free-run embedded commands like:

- `ROTATE_LEFT`
- `ROTATE_RIGHT`
- `MOVE_FORWARD`
- `MOVE_BACKWARD`

### 3.7 Behaviour Contexts / Modes

The system effectively has three behaviour contexts:

```text
Autonomous embedded mode:
  Sun tracking and rain protection run locally.

Manual/free-run remote mode:
  User sends direct commands such as STOP, ROTATE_LEFT, MOVE_FORWARD.

Grid/backend mode:
  User sends map-based commands such as PLACE, MOVE, LEFT, RIGHT, STEP_BACK.
```

These do not need to be one explicit enum, but the system should behave as if these contexts exist.

### 3.8 Jobs

A job is one executable command request.

Examples:

- `STOP`
- `REPORT`
- `MOVE`
- `LEFT`
- `SET_MODE_FAST`

Required behaviours:

- User/frontend creates job.
- Backend validates permission/capability/map/state.
- Backend queues job.
- Nano/adapter polls and claims job.
- Nano/adapter marks job started.
- Nano/adapter marks job completed or failed.
- Backend stores execution result.

### 3.9 Workflows

A workflow is an ordered list of commands.

Example workflow:

```text
PLACE 0,0,North
MOVE
RIGHT
MOVE
REPORT
```

Required behaviours:

- User creates workflow.
- Backend stores workflow.
- Backend stores child jobs/steps.
- Adapter claims workflow.
- Adapter executes steps in order.
- Backend records workflow result.
- Workflow may be `BestEffort` or `AllOrNothing`.

### 3.10 Job History

Every command execution should be logged.

Job history should record:

- Job ID.
- Device ID.
- Command name.
- Payload.
- Whether it executed.
- Whether it succeeded.
- Result JSON.
- Failure code/message.
- Timestamp.

Important rule:

```text
JobHistory records what actually happened.
```

Rollback must use history, not the original request.

### 3.11 Workflow History

Workflow execution should be logged.

Workflow history should record:

- Workflow ID.
- Device ID.
- Status.
- Whether it executed.
- Whether it succeeded.
- Started time.
- Completed time.
- Failed step number if applicable.
- Failure message if applicable.

Detailed step results remain in `JobHistory`.

### 3.12 Rollback

Rollback generates compensating work from successful history.

Example mappings:

```text
MOVE      -> STEP_BACK
STEP_BACK -> MOVE
LEFT      -> RIGHT
RIGHT     -> LEFT
REPORT    -> no rollback
STOP      -> no rollback
PLACE     -> rollback boundary
```

Rules:

- Rollback uses actual successful `JobHistory`.
- Failed/non-executed commands are ignored.
- Rollback order is reversed.
- Rollback jobs/workflows are normal executable work.
- Rollback work should be marked as rollback work.
- Rollback should be auditable.

### 3.13 Rollback Request Audit

Rollback requests should be tracked.

A rollback request should record:

- Who requested rollback.
- Which history rows were targeted.
- Whether generation succeeded or failed.
- Generated rollback job/workflow ID.
- Failure reason if rollback generation failed.

---

## 4. Dashboard Features

The dashboard is the user-facing control and monitoring interface.

### 4.1 Live Robot Status

Dashboard should show:

- Robot name.
- Online/offline/stale state.
- Current operational mode.
- Current status message.
- Last seen time.
- Last heartbeat time.
- Current fault/error if any.

### 4.2 Latest Telemetry

Dashboard should show:

- Rain detected.
- Rain raw value.
- Left/right brightness.
- Line sensor states.
- Sun mode FAST/SLOW.
- Wi-Fi signal.
- Current mode.
- Latest telemetry timestamp.

### 4.3 Grid / Map View

Dashboard should show:

- Assigned map.
- Grid rows/columns.
- Current `GridX`.
- Current `GridY`.
- Current facing direction.
- Whether grid pose is trusted.
- Whether robot is grid aligned.

Dashboard should allow:

- `PLACE x,y,direction`
- Grid command buttons.
- Workflow creation.
- Rollback from history.

### 4.4 Remote Control Panel

Dashboard should expose buttons for:

- `STOP`
- `AUTO`
- `REPORT`
- `SET_MODE_FAST`
- `SET_MODE_SLOW`
- `RETURN_TO_SAFE_ZONE`
- `ROTATE_LEFT`
- `ROTATE_RIGHT`
- `MOVE_FORWARD`
- `MOVE_BACKWARD`
- `PLACE`
- `MOVE`
- `LEFT`
- `RIGHT`
- `STEP_BACK`

Important rule:

```text
Frontend creates jobs/workflows.
Nano/adapter polls and executes them.
```

The frontend should not directly control the Nano.

### 4.5 Job / Workflow History

Dashboard should show:

- Recent jobs.
- Recent workflows.
- Job status.
- Workflow status.
- Claimed/completed times.
- Failure messages.
- Rollback availability.

### 4.6 Telemetry History

Dashboard should show:

- Recent telemetry readings.
- Rain events.
- Drying condition trend.
- Light trend.
- Connectivity history.

### 4.7 Laundry Active / Inactive Toggle

Dashboard should let users mark whether laundry is currently outside.

Rules:

- If laundry is inactive, rain notifications should not be sent.
- If laundry is active, rain/safety alerts may be sent.
- This prevents pointless alert spam.

---

## 5. Notification Features

Notifications are managed by backend, not directly by Nano.

### 5.1 Rain Notifications

Notify users when rain is confirmed.

Example message:

```text
Rain detected. Robot is moving to shade.
```

Rules:

- Only send if laundry is active.
- Avoid repeated spam using cooldown.
- Send only after rain is confirmed.
- Do not depend on frontend being open.

### 5.2 Safe Zone Notifications

Notify users when the robot reaches safe zone.

Example:

```text
Robot reached the safe area.
```

### 5.3 Failure Notifications

Notify if important protection behaviour fails.

Examples:

```text
Robot failed to reach shade.
Robot reported a motor fault.
Rain detected but robot is offline.
```

### 5.4 Offline / Stale Data Notifications

Notify if robot loses communication while laundry is active.

Example:

```text
Robot connection lost. Last update was 6 minutes ago.
```

Rules:

- Backend uses `LastSeenAtUtc` / `LastHeartbeatAtUtc`.
- Dashboard should show stale/offline state.
- Notification should not spam repeatedly.

### 5.5 Shared-Household Notifications

System should support housemate/user routing.

Features:

- Registered users/housemates.
- Users linked to device/household.
- Notifications only sent to relevant users.
- Optional future at-home filtering.

---

## 6. Fallback and Failure Behaviour

### 6.1 Nano Offline from Backend

If Nano cannot reach backend/Pi:

- Nano keeps local autonomy running.
- Nano keeps detecting rain.
- Nano keeps following line to shade.
- Nano keeps stopping motors safely.
- Nano buffers important events.
- Nano retries connection.
- Nano uploads buffered events once reconnected.

### 6.2 Backend Stale State

If backend has not heard from robot recently:

- Mark robot as stale/offline.
- Dashboard shows warning.
- Notify users if laundry is active.
- Continue accepting queued commands, but mark that robot is offline.

### 6.3 Queued Commands While Offline

If user sends command while robot is offline:

- Backend stores command as queued.
- Dashboard shows command is queued.
- Robot executes it only after reconnecting and polling.

Important note:

```text
STOP cannot be delivered immediately if the robot is disconnected.
```

The dashboard must make this clear.

### 6.4 Local Safety Still Wins

Even when backend/frontend sends commands:

- Rain protection overrides unsafe movement.
- STOP remains highest priority when received.
- Robot can reject unsafe commands.
- Rejected jobs should be marked failed with clear reason.

---

## 7. Security and Access Features

### 7.1 Human Authentication

Human users authenticate using Basic Auth.

User data:

- `AppUser`
- `AppUserCredential`

Passwords are stored as hashes, not raw text.

### 7.2 Device Authentication

Nano/adapter authenticates using device credentials.

Headers:

```http
X-Device-Credential-Id: <credentialIdentifier>
X-Device-Credential-Secret: <rawSecret>
```

Device secrets are hashed in the backend.

### 7.3 Device Permissions

Users should have permissions per device.

Permission levels:

- `Viewer`
- `Operator`
- `Manager`
- `Owner`
- `Admin`

Expected behaviour:

- Viewer can view status/telemetry.
- Operator can create jobs/workflows.
- Manager can manage device settings/credentials.
- Owner can manage permissions.
- Admin can manage everything.

---

## 8. Validation Features

### 8.1 Embedded Validation

Validate:

- Rain detection time.
- Time from rain detection to movement start.
- Line-following reliability.
- Sun-tracking behaviour.
- Wi-Fi reconnect behaviour.
- Event buffering during communication failure.
- FAST/SLOW demo modes.
- Upload success/failure count.

### 8.2 Backend Validation

Validate:

- Admin login works.
- Normal user cannot self-register as admin.
- Device permissions work.
- Device credentials work.
- Wrong device credential fails.
- Jobs can be created/claimed/completed.
- Workflows can be created/claimed/completed.
- Telemetry updates `DeviceStatus`.
- Rollback generation works from successful history.
- Cleanup/test data does not corrupt seed data.

### 8.3 Dashboard Validation

Validate:

- User can understand current status.
- User can identify rain state.
- User can see online/offline state.
- User can create safe commands.
- User can view grid pose.
- User can create workflows.
- User can request rollback.
- Alerts are timely and useful.

---

## 9. Final Feature Summary

```text
Embedded Features
├── Sun tracking
├── Rain detection
├── Rain override/protection mode
├── Line-following to shade
├── Local autonomy during communication failure
├── Telemetry generation
├── Event buffering
├── Remote command handling
├── Non-blocking real-time loop
└── Debug/validation support

Backend Features
├── Robot-neutral device model
├── Command catalogue
├── Device capabilities
├── Device status
├── Device credentials
├── Device permissions
├── Map/grid support
├── Grid pose and alignment tracking
├── Jobs
├── Workflows
├── Job history
├── Workflow history
├── Rollback
└── Notification logic

Dashboard Features
├── Live status view
├── Latest telemetry view
├── Grid/map view
├── Remote control panel
├── Job/workflow history
├── Telemetry history
├── Rollback controls
└── Laundry active/inactive toggle

Notification/Fallback Features
├── Rain alerts
├── Safe-zone alerts
├── Failure alerts
├── Offline/stale alerts
├── Shared-household routing
├── Nano-side event buffering
├── Backend stale-state warning
└── Queued commands while robot is offline
```


From the Nano, localhost means the Nano itself.

For local development, run your API on your laptop’s network IP using HTTP:

dotnet run --urls "http://0.0.0.0:5232;https://localhost:7232"

Then find your laptop IP:

ipconfig

Use your Wi-Fi IPv4 address in the sketch:

const char API_HOST[] = "192.168.x.x";
const int API_PORT = 5232;

If Windows blocks it, allow the port:

New-NetFirewallRule -DisplayName "RobotControllerApi HTTP 5232" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5232

Before using the Nano, test from a phone/laptop on the same Wi-Fi:

http://<your-laptop-ip>:5232/swagger

If that page does not load, the Nano will not reach the API either.


PS C:\Users\Manit Khera\Documents\Studies everything\SIT331-Submissions\5.2HD\RobotControllerApi\RobotControllerApi> $env:ASPNETCORE_ENVIRONMENT="Development"
>> dotnet run --no-launch-profile --urls "http://0.0.0.0:5232"

