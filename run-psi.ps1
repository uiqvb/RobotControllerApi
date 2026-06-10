$ErrorActionPreference = "Stop"
newman.cmd run "RobotControllerApi.refactor_smoke.safe.v2.postman_collection.json" -e "RobotControllerApi.refactor_smoke.safe.v2.postman_environment.json" --insecure
