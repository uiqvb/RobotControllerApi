# RobotControllerApi Newman Tests

This folder contains a Postman/Newman test suite for your current RobotControllerApi codebase.

## Files

```text
RobotControllerApi.postman_collection.json
RobotControllerApi.local.postman_environment.json
run-newman.ps1
```

## What it tests

The collection tests the main backend chain:

```text
Admin Basic Auth
Anonymous access blocking
Temporary normal user registration
Normal user role bootstrap rule
DevicePermission CRUD
Temporary command/map/device/capability CRUD
DeviceCredential creation
DeviceCredential adapter authentication
Wrong robot secret rejection
Wrong device route rejection
Job create -> claim -> started -> completed
Workflow create -> claim -> started -> completed
Telemetry POST -> latest -> summary -> DeviceStatus update
Cleanup
```

It also includes non-blocking authorization gap checks for endpoints that currently only require Basic Auth but may eventually need stricter role/permission checks.

## Setup

Install Newman if needed:

```powershell
npm install -g newman
```

Open `RobotControllerApi.local.postman_environment.json` and set:

```text
adminPassword = your current admin password
```

The default base URL is:

```text
https://localhost:7232
```

## Run

From this folder:

```powershell
.\run-newman.ps1
```

Or manually:

```powershell
newman run RobotControllerApi.postman_collection.json `
  -e RobotControllerApi.local.postman_environment.json `
  --insecure
```

`--insecure` is required for local HTTPS dev certificates.

## Self-cleaning behavior

The collection creates records with a unique run id:

```text
NEWMAN_TEST_<runId>
```

At the end it attempts to delete:

```text
temporary workflow
temporary job
temporary credential
temporary device permission
temporary device capability
temporary device
temporary map
temporary command
temporary user
```

Telemetry is posted only against the temporary device, so it should be removed when the temporary device is deleted if your schema keeps `telemetryreading.deviceid ON DELETE CASCADE`.

## Authorization gap checks

The environment variable is:

```text
strictAuthorizationTests = false
```

When `false`, the collection logs warnings for expected authorization gaps but does not fail the run.

Set it to `true` when you want these to become hard failures:

```text
strictAuthorizationTests = true
```

The checked areas are:

```text
normal user listing device credentials
normal user listing device permissions
normal user listing all jobs
```

Based on the code you uploaded, some of these may currently pass for a normal authenticated user because the controllers use Basic Auth but not Admin/device-permission policies yet.

## Expected early failures

If the first request fails:

```text
Initialize run and check Swagger
```

then your API is probably not running at `https://localhost:7232` or the HTTPS dev cert is causing trouble.

If admin auth fails:

```text
Admin login endpoint works
```

then fix `adminPassword` in the environment file.

If cleanup leaves records behind, manually inspect using pgAdmin. The collection is self-cleaning for normal successful runs, but a hard crash halfway through can leave temporary records.


---

## v2 fixes

This version fixes the remaining false negatives from the previous run:

1. The job completion request body now escapes `resultJson` correctly.
2. The workflow completion request body now escapes `resultJson` correctly.
3. `/api/devices/{deviceId}/telemetry/latest` is now tested as a raw `TelemetryReadingResponse`.
4. `/api/devices/{deviceId}/telemetry/summary` is now tested for connection/status summary fields.

The previous collection expected `connectionState` on `/telemetry/latest`, but the current API returns that field from `/telemetry/summary`, not `/telemetry/latest`.
