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
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS production

# curl is here solely for HEALTHCHECK; the runtime image ships without one.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Run as an unprivileged user. Created explicitly rather than relying on the
# base image's built-in app user, so the UID is stable and visible here.
RUN groupadd --system --gid 5678 appgroup \
    && useradd --system --uid 5678 --gid appgroup --no-create-home appuser

WORKDIR /app
COPY --from=publish --chown=appuser:appgroup /app/publish .

# Configuration only - no credentials, no connection string, no environment
# identity. Everything environment-specific arrives at `docker run` time, which
# is what lets one image serve as both staging and production.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

EXPOSE 8080

USER appuser

# Shell form on purpose: the port has to expand at run time, so overriding
# ASPNETCORE_HTTP_PORTS moves the probe with the listener.
HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=5 \
    CMD curl -fsS "http://localhost:${ASPNETCORE_HTTP_PORTS}/health" || exit 1

ENTRYPOINT ["dotnet", "RobotControllerApi.dll"]
