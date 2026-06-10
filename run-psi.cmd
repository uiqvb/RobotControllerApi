@echo off
setlocal
where newman.cmd >nul 2>nul
if errorlevel 1 (
  echo Newman was not found. Install it with: npm install -g newman
  exit /b 1
)
newman.cmd run "RobotControllerApi.refactor_smoke.safe.v2.postman_collection.json" -e "RobotControllerApi.refactor_smoke.safe.v2.postman_environment.json" --insecure
exit /b %ERRORLEVEL%
