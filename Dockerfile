# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Stage 1: restore + build (SDK present, never shipped)
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# The project file alone first, so `dotnet restore` is cached against dependency
# changes rather than against every source edit.
COPY RobotControllerApi.csproj ./
COPY tests/RobotControllerApi.Tests/RobotControllerApi.Tests.csproj tests/RobotControllerApi.Tests/
RUN dotnet restore RobotControllerApi.csproj

COPY . .
RUN dotnet build RobotControllerApi.csproj -c Release --no-restore

# ---------------------------------------------------------------------------
# Stage 2: test - the SDK, the dev dependencies and the full source tree.
#
# `docker build --target test` produces an image the pipeline can run a test
# suite inside. It keeps everything the production image drops: the SDK, the
# test host, and the sources a test project compiles against.
#
# The suite lives in tests/RobotControllerApi.Tests and is restored and built
# here, so `docker run` on this image reports a real, non-zero test count.
# ---------------------------------------------------------------------------
FROM build AS test

# The test project and its dev-only dependencies, restored and built here rather
# than in the shared build stage so none of it can reach the production image.
RUN dotnet restore tests/RobotControllerApi.Tests/RobotControllerApi.Tests.csproj \
    && dotnet build tests/RobotControllerApi.Tests/RobotControllerApi.Tests.csproj -c Release --no-restore

# The test project, so a bare `dotnet test` resolves to it and the pipeline's
# commands work verbatim:
#   docker run --rm myapp:test-1.0.1 dotnet test --filter "Category!=Integration"
#   docker run --rm myapp:test-1.0.1 dotnet test --filter "Category=Integration"
# Coverage tooling, pinned. Stage 2 of the pipeline uses both:
#   dotnet-coverage  - instruments the running API while the integration suite
#                      calls it, which coverlet cannot do across a process boundary
#   reportgenerator  - merges unit + integration coverage into one report
RUN dotnet tool install --global dotnet-coverage --version 18.11.2 \
    && dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.5.11
ENV PATH="${PATH}:/root/.dotnet/tools"

WORKDIR /src/tests/RobotControllerApi.Tests

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    API_BASE_URL=http://robot-test-app:8080

CMD ["dotnet", "test", "-c", "Release", "--no-build", "--filter", "Category!=Integration", "--logger:junit;LogFilePath=/src/TestResults/unit-results.xml"]

# ---------------------------------------------------------------------------
# Stage 3: publish - trimmed, runtime-ready output
# ---------------------------------------------------------------------------
FROM build AS publish
RUN dotnet publish RobotControllerApi.csproj -c Release -o /app/publish --no-build

# ---------------------------------------------------------------------------
# Stage 4: production - runtime only. No SDK, no sources, no test tooling.
#
# Chiselled Ubuntu (security remediation M1 in security-findings.md). The image
# contains the .NET runtime and its native dependencies and nothing else: no
# shell, no package manager, no curl, perl, util-linux or ncurses. The Debian
# base this replaced carried 4 CRITICAL and 63 HIGH findings, all in packages
# the app never used. Removing the packages removes the findings, rather than
# accepting them. "-extra" adds ICU and tzdata so culture and time-zone
# behaviour is unchanged from the Debian image.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled-extra AS production

WORKDIR /app

# The image's built-in non-root user, UID 1654 ($APP_UID in the base image).
# There is no shell to run useradd with, so the base image's user is used. It
# has no login shell and no home directory contents.
COPY --from=publish --chown=1654:1654 /app/publish .

# Configuration only - no credentials, no connection string, no environment
# identity. Everything environment-specific arrives at `docker run` time, which
# is what lets one image serve as both staging and production.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

EXPOSE 8080

USER 1654

# Exec form: there is no shell in this image. The app runs itself in probe mode
# (see --healthcheck at the top of Program.cs), which reads ASPNETCORE_HTTP_PORTS
# so overriding the port still moves the probe with the listener. The compose
# files' depends_on: service_healthy and the pipeline's deploy verification both
# rely on this status.
HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=5 \
    CMD ["dotnet", "/app/RobotControllerApi.dll", "--healthcheck"]

ENTRYPOINT ["dotnet", "RobotControllerApi.dll"]
