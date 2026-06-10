# RobotControllerApi + Nano 33 IoT Handoff Notes

Use this file when continuing in a new ChatGPT chat or when restarting the local setup.

---

## 1. Current System State

The backend + Nano integration now works for the important basic path.

Confirmed working:

```text
Nano connects to phone hotspot / Wi-Fi
Nano reaches RobotControllerApi over HTTP
Device credential authentication works
Nano posts telemetry successfully
TelemetryReading table receives rows
DeviceStatus updates
Nano polls claim-next
Backend creates jobs
Nano claims jobs
Nano parses job response correctly after v8 fix
Nano executes REPORT
Nano marks job started
Nano marks job completed
```

Latest successful Serial Monitor proof:

```text
Claimed work body: {"workItemType":"Job","job":{"id":15,...,"commandCatalogueId":10,...},"workflow":null}
Executing job 15 command REPORT
Mark job started status: 204
Mark job completed status: 204
```

Important Nano sketch version:

```text
SmartClotheslineNanoApiAdapter_FIXED_v8.ino
```

Important v8 fix:

```text
The API returns "workflow": null for normal job responses.
Older Nano code wrongly treated this as a workflow.
v8 only treats it as workflow when workflow is a real JSON object.
```

---

## 2. Mental Model

The system is:

```text
Frontend / Swagger / curl
    ↓
RobotControllerApi backend
    ↓
PostgreSQL DB
    ↓
Nano 33 IoT polls API
    ↓
Robot motors + sensors
```

The Nano is the adapter.

It does:

```text
POST telemetry
POST claim-next
PATCH job started
PATCH job completed
PATCH job failed
```

The backend does not directly push commands to the Nano. The Nano polls.

---

## 3. Embedded Features Approved

```text
Dry weather:
  Read left/right brightness and rotate toward brighter side.

Rain:
  Confirm rain, stop sun tracking, follow black line to shade/safe area.

Local safety:
  Continue rain detection and protection even if API/Wi-Fi fails.

Telemetry:
  Send mode, rainRaw, rainDetected, luxLeft/luxRight, line sensors, sunMode, wifiRssi.

Event buffering:
  Buffer BOOT, RAIN_CONFIRMED, SAFE_ZONE_REACHED, WIFI_LOST, WIFI_RECOVERED.

Remote commands:
  REPORT
  STOP
  AUTO
  SET_MODE_FAST
  SET_MODE_SLOW
  RETURN_TO_SAFE_ZONE
  ROTATE_LEFT
  ROTATE_RIGHT
  MOVE_FORWARD
  MOVE_BACKWARD

Safety:
  Rain/protection mode can reject unsafe manual movement.
```

---

## 4. Backend / Grid Features Planned

Backend unit features still planned:

```text
Map/grid support
Grid pose: x, y, facing, trusted/aligned
PLACE x,y,direction
MOVE = move one grid cell
LEFT / RIGHT = rotate grid orientation
STEP_BACK = reverse one grid cell
Workflows
Workflow history
Rollback
Dashboard map view
```

Important distinction:

```text
MOVE_FORWARD = manual physical pulse, not map-aware.
MOVE = grid command, moves one logical grid cell and supports rollback.
```

---

## 5. Start the Local API

From the backend project folder:

```powershell
cd "C:\Users\Manit Khera\Documents\Studies everything\SIT331-Submissions\5.2HD\RobotControllerApi\RobotControllerApi"
```

Run API for Nano testing:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --no-launch-profile --urls "http://0.0.0.0:5232"
```

Expected:

```text
Now listening on: http://0.0.0.0:5232
```

Do not use HTTPS for the Nano during local testing.

Swagger:

```text
http://localhost:5232/swagger
```

If using phone hotspot, get laptop IP:

```powershell
ipconfig
```

Use the Wi-Fi adapter IPv4. Example hotspot IP used previously:

```text
192.168.143.146
```

Nano sketch must use that:

```cpp
const char API_HOST[] = "192.168.143.146";
const int API_PORT = 5232;
```

---

## 6. Windows Firewall Setup

Run PowerShell as Administrator:

```powershell
New-NetFirewallRule -DisplayName "RobotControllerApi HTTP 5232" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5232 -Profile Any
```

Optional, also allow dotnet:

```powershell
New-NetFirewallRule -DisplayName "Allow dotnet inbound" -Direction Inbound -Program "C:\Program Files\dotnet\dotnet.exe" -Action Allow -Profile Any
```

Optional, make Wi-Fi private:

```powershell
Set-NetConnectionProfile -InterfaceAlias "Wi-Fi" -NetworkCategory Private
```

Check API binding:

```powershell
netstat -ano | findstr :5232
```

Good:

```text
0.0.0.0:5232 LISTENING
```

---

## 7. Basic API Reachability Tests

From the laptop:

```powershell
curl.exe -i http://localhost:5232/api/auth/me
```

Expected:

```text
HTTP/1.1 401 Unauthorized
```

That is good. It means API is alive but auth is required.

From laptop using hotspot/Wi-Fi IP:

```powershell
curl.exe -i http://192.168.143.146:5232/api/auth/me
```

Expected:

```text
HTTP/1.1 401 Unauthorized
```

Swagger test:

```powershell
curl.exe -i http://localhost:5232/swagger/index.html
curl.exe -i http://192.168.143.146:5232/swagger/index.html
```

Expected:

```text
HTTP/1.1 200 OK
```

---

## 8. Current Known Command IDs

Current `commandcatalogue` IDs:

```text
1  AUTO
2  STOP
3  ROTATE_LEFT
4  ROTATE_RIGHT
5  MOVE_FORWARD
7  RETURN_TO_SAFE_ZONE
8  SET_MODE_FAST
9  SET_MODE_SLOW
10 REPORT
```

Verify anytime:

```sql
SELECT id, name
FROM public.commandcatalogue
ORDER BY id;
```

---

## 9. PowerShell Variables for Testing

Use placeholders for secrets if storing this file publicly.

```powershell
$base = "http://localhost:5232"
$adminEmail = "admin@test.com"
$adminPassword = "<ADMIN_PASSWORD>"

$deviceId = 1
$credentialId = "<DEVICE_CREDENTIAL_IDENTIFIER>"
$credentialSecret = "<DEVICE_CREDENTIAL_SECRET>"

$AUTO = 1
$STOP = 2
$ROTATE_LEFT = 3
$ROTATE_RIGHT = 4
$MOVE_FORWARD = 5
$RETURN_TO_SAFE_ZONE = 7
$SET_MODE_FAST = 8
$SET_MODE_SLOW = 9
$REPORT = 10
```

Known local-dev credential previously used for device 1:

```text
credentialIdentifier = cred_a5041e7c2a7646ce92991abc9ed9d133
secret = <raw secret from your saved local notes>
```

---

## 10. Test Device Credential / Claim-Next Manually

Expected if no jobs queued:

```powershell
curl.exe -i -X POST "$base/api/adapter/devices/$deviceId/work-items/claim-next" `
  -H "Content-Type: application/json" `
  -H "X-Device-Credential-Id: $credentialId" `
  -H "X-Device-Credential-Secret: $credentialSecret" `
  --data-binary "{}"
```

Expected:

```text
HTTP/1.1 204 No Content
```

If it returns `401`, the device credential is wrong, revoked, or belongs to another device.

---

## 11. Create Jobs with curl

REPORT:

```powershell
$body = '{"commandCatalogueId":10,"payloadJson":"{}"}'

curl.exe -i -X POST "$base/api/devices/$deviceId/jobs" `
  -u "$($adminEmail):$($adminPassword)" `
  -H "Content-Type: application/json" `
  --data-binary $body
```

STOP:

```powershell
$body = '{"commandCatalogueId":2,"payloadJson":"{}"}'

curl.exe -i -X POST "$base/api/devices/$deviceId/jobs" `
  -u "$($adminEmail):$($adminPassword)" `
  -H "Content-Type: application/json" `
  --data-binary $body
```

AUTO:

```powershell
$body = '{"commandCatalogueId":1,"payloadJson":"{}"}'

curl.exe -i -X POST "$base/api/devices/$deviceId/jobs" `
  -u "$($adminEmail):$($adminPassword)" `
  -H "Content-Type: application/json" `
  --data-binary $body
```

SET_MODE_SLOW:

```powershell
$body = '{"commandCatalogueId":9,"payloadJson":"{}"}'

curl.exe -i -X POST "$base/api/devices/$deviceId/jobs" `
  -u "$($adminEmail):$($adminPassword)" `
  -H "Content-Type: application/json" `
  --data-binary $body
```

SET_MODE_FAST:

```powershell
$body = '{"commandCatalogueId":8,"payloadJson":"{}"}'

curl.exe -i -X POST "$base/api/devices/$deviceId/jobs" `
  -u "$($adminEmail):$($adminPassword)" `
  -H "Content-Type: application/json" `
  --data-binary $body
```

MOVE_FORWARD:

```powershell
$body = '{"commandCatalogueId":5,"payloadJson":"{}"}'

curl.exe -i -X POST "$base/api/devices/$deviceId/jobs" `
  -u "$($adminEmail):$($adminPassword)" `
  -H "Content-Type: application/json" `
  --data-binary $body
```

RETURN_TO_SAFE_ZONE:

```powershell
$body = '{"commandCatalogueId":7,"payloadJson":"{}"}'

curl.exe -i -X POST "$base/api/devices/$deviceId/jobs" `
  -u "$($adminEmail):$($adminPassword)" `
  -H "Content-Type: application/json" `
  --data-binary $body
```

---

## 12. Expected Nano Serial Output for Working Job

For REPORT:

```text
Claimed work body: {"workItemType":"Job","job":{...},"workflow":null}
Executing job <id> command REPORT
Mark job started status: 204
Mark job completed status: 204
```

If job gets stuck as Claimed, inspect the Serial output.

Known fixed issues:

```text
415 Unsupported Media Type:
  claim-next needed body "{}"

400 Payload must be a JSON object:
  telemetry needed { "payload": {...}, "providerType": "Poll" }

Chunked response:
  Nano needed to decode Transfer-Encoding: chunked

workflow:null bug:
  Nano wrongly treated normal job as workflow. Fixed in v8.
```

---

## 13. SQL: Check Telemetry

Latest telemetry:

```sql
SELECT
    id,
    deviceid,
    payloadjson,
    providertype,
    recordedatutc,
    createddate
FROM public.telemetryreading
WHERE deviceid = 1
ORDER BY id DESC
LIMIT 10;
```

Device status:

```sql
SELECT
    deviceid,
    connectionstate,
    operationalstate,
    lastseenatutc,
    lastheartbeatatutc,
    statusmessage,
    modifieddate
FROM public.devicestatus
WHERE deviceid = 1;
```

Telemetry count:

```sql
SELECT COUNT(*)
FROM public.telemetryreading
WHERE deviceid = 1;
```

Delete all telemetry for device 1 if testing spam gets too large:

```sql
DELETE FROM public.telemetryreading
WHERE deviceid = 1;
```

Keep only latest 200 rows:

```sql
DELETE FROM public.telemetryreading
WHERE deviceid = 1
AND id NOT IN (
    SELECT id
    FROM public.telemetryreading
    WHERE deviceid = 1
    ORDER BY id DESC
    LIMIT 200
);
```

---

## 14. SQL: Check Jobs

Latest jobs:

```sql
SELECT
    j.id,
    j.deviceid,
    c.name AS command,
    j.status,
    j.claimedbydevicecredentialid,
    j.claimedatutc,
    j.modifieddate
FROM public.job j
JOIN public.commandcatalogue c ON c.id = j.commandcatalogueid
WHERE j.deviceid = 1
ORDER BY j.id DESC
LIMIT 10;
```

Expected after success:

```text
REPORT | Completed
STOP | Completed
AUTO | Completed
SET_MODE_SLOW | Completed
SET_MODE_FAST | Completed
```

---

## 15. SQL: Check Job History

```sql
SELECT
    id,
    jobid,
    deviceid,
    commandname,
    executed,
    success,
    failurecode,
    failuremessage,
    resultjson,
    createddate
FROM public.jobhistory
WHERE deviceid = 1
ORDER BY id DESC
LIMIT 10;
```

Expected for successful REPORT:

```text
commandname = REPORT
executed = true
success = true
```

Expected for safety override:

```text
commandname = MOVE_FORWARD
success = false
failurecode = LOCAL_SAFETY_OVERRIDE
```

---

## 16. SQL: Cleanup Stuck Jobs

Use when jobs are stuck as `Queued` or `Claimed`.

```sql
DELETE FROM public.jobhistory
WHERE jobid IN (
    SELECT id
    FROM public.job
    WHERE deviceid = 1
    AND status IN ('Queued', 'Claimed')
);

DELETE FROM public.job
WHERE deviceid = 1
AND status IN ('Queued', 'Claimed');
```

---

## 17. SQL: Check Device Credential

```sql
SELECT
    id,
    deviceid,
    name,
    credentialidentifier,
    secretkeyprefix,
    isactive,
    revokedatutc
FROM public.devicecredential
ORDER BY id;
```

Credential must be:

```text
deviceid = 1
isactive = true
revokedatutc = null
```

---

## 18. Admin Lockout Reset

If admin Basic Auth starts returning 401 despite correct password, check lockout:

```sql
SELECT
    id,
    appuserid,
    failedlogincount,
    lockedoutuntilutc
FROM public.appusercredential
ORDER BY id;
```

Reset local dev admin lockout:

```sql
UPDATE public.appusercredential
SET failedlogincount = 0,
    lockedoutuntilutc = NULL,
    modifieddate = now() AT TIME ZONE 'utc'
WHERE appuserid = 1;
```

Test admin:

```powershell
curl.exe -i -u "admin@test.com:<ADMIN_PASSWORD>" http://localhost:5232/api/auth/me
```

Expected:

```text
HTTP/1.1 200 OK
```

---

## 19. Newman Regression Tests

Run Newman:

```powershell
newman run .\RobotControllerApi.postman_collection.json -e .\RobotControllerApi.local.postman_environment.json --insecure --env-var "adminEmail=admin@test.com" --env-var "adminPassword=<ADMIN_PASSWORD>"
```

If PowerShell blocks script:

```powershell
powershell -ExecutionPolicy Bypass -File .\run-newman.ps1
```

---

## 20. Next Development Step

After Nano safe commands are stable, the next target is:

```text
Dashboard MVP
```

Dashboard MVP should include:

```text
Login / Basic Auth
Show device status
Show latest telemetry
Show recent jobs
Buttons:
  REPORT
  STOP
  AUTO
  SET_MODE_FAST
  SET_MODE_SLOW
```

Do not start with grid/workflows/rollback UI yet.

After dashboard MVP:

```text
Manual movement controls
Rain safety rejection test
Offline fallback test
Grid mode
Workflow UI
Rollback UI
Notifications
```

---

## 21. Copy-Paste Prompt for New Chat

Paste this into a new ChatGPT chat:

```text
I am continuing a RobotControllerApi + Arduino Nano 33 IoT smart mobile clothesline robot project.

Current state:
- Backend is ASP.NET Core / C# with PostgreSQL.
- Backend has Basic Auth for human users and device credential auth for robot adapters.
- Newman tests passed for auth, permissions, device credentials, jobs, workflows, telemetry, status, cleanup.
- Nano 33 IoT acts as the adapter.
- Nano connects to Wi-Fi / phone hotspot.
- Nano posts telemetry to /api/adapter/devices/{deviceId}/telemetry.
- Nano polls /api/adapter/devices/{deviceId}/work-items/claim-next.
- Nano marks jobs started/completed/failed.
- Telemetry posts successfully with body:
  { "payload": {...}, "providerType": "Poll" }
- REPORT command is confirmed working:
  Nano claimed a Job with commandCatalogueId 10, executed REPORT, marked started 204, marked completed 204.
- Latest working sketch is SmartClotheslineNanoApiAdapter_FIXED_v8.ino.
- Important previous bug fixes:
  1. claim-next needed JSON body {}
  2. telemetry DTO expects payload, not payloadJson
  3. Nano HTTP client needed to decode chunked responses
  4. workflow:null caused normal jobs to be misclassified as workflows; fixed in v8
- Current command IDs:
  AUTO=1
  STOP=2
  ROTATE_LEFT=3
  ROTATE_RIGHT=4
  MOVE_FORWARD=5
  RETURN_TO_SAFE_ZONE=7
  SET_MODE_FAST=8
  SET_MODE_SLOW=9
  REPORT=10
- API local command:
  $env:ASPNETCORE_ENVIRONMENT="Development"
  dotnet run --no-launch-profile --urls "http://0.0.0.0:5232"
- Phone hotspot laptop IP used before:
  192.168.143.146
- Nano API config:
  API_HOST = "192.168.143.146"
  API_PORT = 5232
  DEVICE_ID = 1

Next goal:
Finish testing safe commands SET_MODE_SLOW, SET_MODE_FAST, STOP, AUTO, then test rain override, safety rejection, offline fallback, and then build dashboard MVP.

Please continue from here without re-explaining the whole architecture.
```


http://192.168.143.146:5232/