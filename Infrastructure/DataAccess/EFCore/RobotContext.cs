using Microsoft.EntityFrameworkCore;
using RobotControllerApi.BoundedContexts.AppUsers.Models;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Models;
using RobotControllerApi.BoundedContexts.DeviceCapabilities.Models;
using RobotControllerApi.BoundedContexts.DeviceCredentials.Models;
using RobotControllerApi.BoundedContexts.DevicePermissions.Models;
using RobotControllerApi.BoundedContexts.Devices.Models;
using RobotControllerApi.BoundedContexts.DeviceStatuses.Models;
using RobotControllerApi.BoundedContexts.JobHistories.Models;
using RobotControllerApi.BoundedContexts.Jobs.Models;
using RobotControllerApi.BoundedContexts.Maps.Models;
using RobotControllerApi.BoundedContexts.Rollbacks.Models;
using RobotControllerApi.BoundedContexts.WorkflowHistories.Models;
using RobotControllerApi.BoundedContexts.Workflows.Models;
using RobotControllerApi.BoundedContexts.Telemetry.Models;
using RobotControllerApi.BoundedContexts.LiveControls.Models;

namespace RobotControllerApi.Infrastructure.DataAccess.EFCore;

public class RobotContext : DbContext
{
    public RobotContext(DbContextOptions<RobotContext> options) : base(options) { }

    public DbSet<CommandCatalogue> CommandCatalogues => Set<CommandCatalogue>();
    public DbSet<Map> Maps => Set<Map>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceCapability> DeviceCapabilities => Set<DeviceCapability>();
    public DbSet<DeviceStatus> DeviceStatuses => Set<DeviceStatus>();
    public DbSet<DeviceCredential> DeviceCredentials => Set<DeviceCredential>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppUserCredential> AppUserCredentials => Set<AppUserCredential>();
    public DbSet<DevicePermission> DevicePermissions => Set<DevicePermission>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<JobHistory> JobHistories => Set<JobHistory>();
    public DbSet<WorkflowHistory> WorkflowHistories => Set<WorkflowHistory>();
    public DbSet<RollbackRequest> RollbackRequests => Set<RollbackRequest>();
    public DbSet<TelemetryReading> TelemetryReadings => Set<TelemetryReading>();
    public DbSet<LiveControlCommand> LiveControlCommands => Set<LiveControlCommand>();
    public DbSet<LiveControlSession> LiveControlSessions => Set<LiveControlSession>();
    public DbSet<LiveControlSegment> LiveControlSegments => Set<LiveControlSegment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommandCatalogue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_commandcatalogue");

            entity.ToTable("commandcatalogue");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");

            entity.Property(e => e.ExecutionKind)
                .HasMaxLength(50)
                .HasColumnName("executionkind");

            entity.Property(e => e.RollbackKind)
                .HasMaxLength(50)
                .HasColumnName("rollbackkind");

            entity.Property(e => e.InverseCommandName)
                .HasMaxLength(100)
                .HasColumnName("inversecommandname");

            entity.Property(e => e.RequiresDuration)
                .HasColumnName("requiresduration");

            entity.Property(e => e.IsActive)
                .HasColumnName("isactive");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");
        });

        modelBuilder.Entity<Map>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_map");

            entity.ToTable("map");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.Property(e => e.Columns)
                .HasColumnName("columns");

            entity.Property(e => e.Rows)
                .HasColumnName("rows");

            entity.Property(e => e.CellSizeCm)
                .HasColumnName("cellsizecm");

            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");

            entity.Property(e => e.IsActive)
                .HasColumnName("isactive");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_device");

            entity.ToTable("device");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.Property(e => e.DeviceIdentifier)
                .HasMaxLength(100)
                .HasColumnName("deviceidentifier");

            entity.Property(e => e.DeviceType)
                .HasMaxLength(100)
                .HasColumnName("devicetype");

            entity.Property(e => e.MapId)
                .HasColumnName("mapid");

            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");

            entity.Property(e => e.IsActive)
                .HasColumnName("isactive");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");

            entity.HasIndex(e => e.DeviceIdentifier)
                .IsUnique(false);
        });

        modelBuilder.Entity<DeviceCapability>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_devicecapability");

            entity.ToTable("devicecapability");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.DeviceId)
                .HasColumnName("deviceid");

            entity.Property(e => e.CommandCatalogueId)
                .HasColumnName("commandcatalogueid");

            entity.Property(e => e.RequiresMap)
                .HasColumnName("requiresmap");

            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");

            entity.Property(e => e.IsActive)
                .HasColumnName("isactive");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");

            entity.HasIndex(e => e.DeviceId);

            entity.HasIndex(e => e.CommandCatalogueId);
        });

        modelBuilder.Entity<DeviceStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_devicestatus");

            entity.ToTable("devicestatus");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.DeviceId)
                .HasColumnName("deviceid");

            entity.Property(e => e.ConnectionState)
                .HasMaxLength(50)
                .HasColumnName("connectionstate");

            entity.Property(e => e.OperationalState)
                .HasMaxLength(50)
                .HasColumnName("operationalstate");

            entity.Property(e => e.LastSeenAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("lastseenatutc");

            entity.Property(e => e.LastHeartbeatAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("lastheartbeatatutc");

            entity.Property(e => e.PoseMapId)
                .HasColumnName("posemapid");

            entity.Property(e => e.GridX)
                .HasColumnName("gridx");

            entity.Property(e => e.GridY)
                .HasColumnName("gridy");

            entity.Property(e => e.Facing)
                .HasMaxLength(50)
                .HasColumnName("facing");

            entity.Property(e => e.IsGridAligned)
                .HasColumnName("isgridaligned");

            entity.Property(e => e.IsGridPoseTrusted)
                .HasColumnName("isgridposetrusted");

            entity.Property(e => e.PoseConfidence)
                .HasColumnName("poseconfidence");

            entity.Property(e => e.EstimatedXcm)
                .HasColumnName("estimatedxcm");

            entity.Property(e => e.EstimatedYcm)
                .HasColumnName("estimatedycm");

            entity.Property(e => e.EstimatedHeadingDegrees)
                .HasColumnName("estimatedheadingdegrees");

            entity.Property(e => e.IsInsideMap)
                .HasColumnName("isinsidemap");

            entity.Property(e => e.StatusMessage)
                .HasMaxLength(1000)
                .HasColumnName("statusmessage");

            entity.Property(e => e.LastErrorCode)
                .HasMaxLength(100)
                .HasColumnName("lasterrorcode");

            entity.Property(e => e.LastErrorMessage)
                .HasMaxLength(1000)
                .HasColumnName("lasterrormessage");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");

            entity.HasIndex(e => e.DeviceId);
        });

        modelBuilder.Entity<DeviceCredential>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_devicecredential");

            entity.ToTable("devicecredential");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.DeviceId)
                .HasColumnName("deviceid");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.Property(e => e.CredentialIdentifier)
                .HasMaxLength(100)
                .HasColumnName("credentialidentifier");

            entity.Property(e => e.SecretKeyPrefix)
                .HasMaxLength(100)
                .HasColumnName("secretkeyprefix");

            entity.Property(e => e.SecretHash)
                .HasMaxLength(500)
                .HasColumnName("secrethash");

            entity.Property(e => e.HashAlgorithm)
                .HasMaxLength(100)
                .HasColumnName("hashalgorithm");

            entity.Property(e => e.ExpiresAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expiresatutc");

            entity.Property(e => e.LastUsedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("lastusedatutc");

            entity.Property(e => e.LastUsedIpAddress)
                .HasMaxLength(100)
                .HasColumnName("lastusedipaddress");

            entity.Property(e => e.LastUsedUserAgent)
                .HasMaxLength(500)
                .HasColumnName("lastuseduseragent");

            entity.Property(e => e.RevokedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("revokedatutc");

            entity.Property(e => e.RevocationReason)
                .HasMaxLength(1000)
                .HasColumnName("revocationreason");

            entity.Property(e => e.IsActive)
                .HasColumnName("isactive");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");

            entity.HasIndex(e => e.DeviceId);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_appuser");

            entity.ToTable("appuser");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.Email)
                .HasMaxLength(320)
                .HasColumnName("email");

            entity.Property(e => e.DisplayName)
                .HasMaxLength(200)
                .HasColumnName("displayname");

            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasColumnName("role");

            entity.Property(e => e.IsActive)
                .HasColumnName("isactive");

            entity.Property(e => e.LastLoginAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("lastloginatutc");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");
        });



        modelBuilder.Entity<AppUserCredential>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_appusercredential");

            entity.ToTable("appusercredential");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.AppUserId)
                .HasColumnName("appuserid");

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(1000)
                .HasColumnName("passwordhash");

            entity.Property(e => e.PasswordHashAlgorithm)
                .HasMaxLength(100)
                .HasColumnName("passwordhashalgorithm");

            entity.Property(e => e.PasswordChangedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("passwordchangedatutc");

            entity.Property(e => e.MustResetPassword)
                .HasColumnName("mustresetpassword");

            entity.Property(e => e.FailedLoginCount)
                .HasColumnName("failedlogincount");

            entity.Property(e => e.LockedOutUntilUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("lockedoutuntilutc");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");

            entity.HasIndex(e => e.AppUserId).IsUnique();
        });

        modelBuilder.Entity<DevicePermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_devicepermission");

            entity.ToTable("devicepermission");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.AppUserId)
                .HasColumnName("appuserid");

            entity.Property(e => e.DeviceId)
                .HasColumnName("deviceid");

            entity.Property(e => e.PermissionLevel)
                .HasMaxLength(50)
                .HasColumnName("permissionlevel");

            entity.Property(e => e.IsActive)
                .HasColumnName("isactive");

            entity.Property(e => e.ExpiresAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expiresatutc");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");

            entity.HasIndex(e => e.AppUserId);

            entity.HasIndex(e => e.DeviceId);
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_job");

            entity.ToTable("job");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.DeviceId)
                .HasColumnName("deviceid");

            entity.Property(e => e.WorkflowId)
                .HasColumnName("workflowid");

            entity.Property(e => e.StepNumber)
                .HasColumnName("stepnumber");

            entity.Property(e => e.CommandCatalogueId)
                .HasColumnName("commandcatalogueid");

            entity.Property(e => e.PayloadJson)
                .HasColumnType("jsonb")
                .HasColumnName("payloadjson");

            entity.Property(e => e.ProviderType)
                .HasMaxLength(50)
                .HasColumnName("providertype");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.Property(e => e.RequestedByAppUserId)
                .HasColumnName("requestedbyappuserid");

            entity.Property(e => e.ClaimedByDeviceCredentialId)
                .HasColumnName("claimedbydevicecredentialid");

            entity.Property(e => e.ClaimedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("claimedatutc");

            entity.Property(e => e.LeaseExpiresAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("leaseexpiresatutc");

            entity.Property(e => e.IsRollback)
                .HasColumnName("isrollback");

            entity.Property(e => e.RollbackOfJobHistoryId)
                .HasColumnName("rollbackofjobhistoryid");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");

            entity.HasIndex(e => e.DeviceId);

            entity.HasIndex(e => e.WorkflowId);

            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_workflow");

            entity.ToTable("workflow");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.DeviceId)
                .HasColumnName("deviceid");

            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");

            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");

            entity.Property(e => e.SchemaVersion)
                .HasMaxLength(50)
                .HasColumnName("schemaversion");

            entity.Property(e => e.ExecutionMode)
                .HasMaxLength(50)
                .HasColumnName("executionmode");

            entity.Property(e => e.ProviderType)
                .HasMaxLength(50)
                .HasColumnName("providertype");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.Property(e => e.RequestedByAppUserId)
                .HasColumnName("requestedbyappuserid");

            entity.Property(e => e.ClaimedByDeviceCredentialId)
                .HasColumnName("claimedbydevicecredentialid");

            entity.Property(e => e.ClaimedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("claimedatutc");

            entity.Property(e => e.LeaseExpiresAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("leaseexpiresatutc");

            entity.Property(e => e.IsRollback)
                .HasColumnName("isrollback");

            entity.Property(e => e.RollbackOfWorkflowHistoryId)
                .HasColumnName("rollbackofworkflowhistoryid");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");

            entity.HasIndex(e => e.DeviceId);

            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<JobHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_jobhistory");

            entity.ToTable("jobhistory");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.JobId)
                .HasColumnName("jobid");

            entity.Property(e => e.WorkflowId)
                .HasColumnName("workflowid");

            entity.Property(e => e.StepNumber)
                .HasColumnName("stepnumber");

            entity.Property(e => e.DeviceId)
                .HasColumnName("deviceid");

            entity.Property(e => e.CommandCatalogueId)
                .HasColumnName("commandcatalogueid");

            entity.Property(e => e.CommandName)
                .HasMaxLength(100)
                .HasColumnName("commandname");

            entity.Property(e => e.PayloadJson)
                .HasColumnType("jsonb")
                .HasColumnName("payloadjson");

            entity.Property(e => e.ProviderType)
                .HasMaxLength(50)
                .HasColumnName("providertype");

            entity.Property(e => e.ExecutionKind)
                .HasMaxLength(50)
                .HasColumnName("executionkind");

            entity.Property(e => e.RollbackKind)
                .HasMaxLength(50)
                .HasColumnName("rollbackkind");

            entity.Property(e => e.Executed)
                .HasColumnName("executed");

            entity.Property(e => e.Success)
                .HasColumnName("success");

            entity.Property(e => e.ResultJson)
                .HasColumnType("jsonb")
                .HasColumnName("resultjson");

            entity.Property(e => e.FailureCode)
                .HasMaxLength(100)
                .HasColumnName("failurecode");

            entity.Property(e => e.FailureMessage)
                .HasMaxLength(1000)
                .HasColumnName("failuremessage");

            entity.Property(e => e.StartedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("startedatutc");

            entity.Property(e => e.CompletedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("completedatutc");

            entity.Property(e => e.DurationMs)
                .HasColumnName("durationms");

            entity.Property(e => e.RollbackOfJobHistoryId)
                .HasColumnName("rollbackofjobhistoryid");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.HasIndex(e => e.JobId);

            entity.HasIndex(e => e.WorkflowId);

            entity.HasIndex(e => e.DeviceId);
        });

        modelBuilder.Entity<WorkflowHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_workflowhistory");

            entity.ToTable("workflowhistory");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.WorkflowId)
                .HasColumnName("workflowid");

            entity.Property(e => e.DeviceId)
                .HasColumnName("deviceid");

            entity.Property(e => e.ProviderType)
                .HasMaxLength(50)
                .HasColumnName("providertype");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.Property(e => e.Executed)
                .HasColumnName("executed");

            entity.Property(e => e.Success)
                .HasColumnName("success");

            entity.Property(e => e.StartedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("startedatutc");

            entity.Property(e => e.CompletedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("completedatutc");

            entity.Property(e => e.FailedStepNumber)
                .HasColumnName("failedstepnumber");

            entity.Property(e => e.FailureMessage)
                .HasMaxLength(1000)
                .HasColumnName("failuremessage");

            entity.Property(e => e.RollbackOfWorkflowHistoryId)
                .HasColumnName("rollbackofworkflowhistoryid");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.HasIndex(e => e.WorkflowId);

            entity.HasIndex(e => e.DeviceId);
        });

        modelBuilder.Entity<RollbackRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_rollbackrequest");

            entity.ToTable("rollbackrequest");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.DeviceId)
                .HasColumnName("deviceid");

            entity.Property(e => e.RequestedByAppUserId)
                .HasColumnName("requestedbyappuserid");

            entity.Property(e => e.RequestedByProvider)
                .HasMaxLength(50)
                .HasColumnName("requestedbyprovider");

            entity.Property(e => e.TargetType)
                .HasMaxLength(50)
                .HasColumnName("targettype");

            entity.Property(e => e.TargetIdsJson)
                .HasColumnType("jsonb")
                .HasColumnName("targetidsjson");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.Property(e => e.GeneratedWorkflowId)
                .HasColumnName("generatedworkflowid");

            entity.Property(e => e.GeneratedJobId)
                .HasColumnName("generatedjobid");

            entity.Property(e => e.FailureCode)
                .HasMaxLength(100)
                .HasColumnName("failurecode");

            entity.Property(e => e.FailureMessage)
                .HasMaxLength(1000)
                .HasColumnName("failuremessage");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");

            entity.HasIndex(e => e.DeviceId);

            entity.HasIndex(e => e.Status);
        });


        modelBuilder.Entity<LiveControlCommand>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_livecontrolcommand");

            entity.ToTable("livecontrolcommand");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.DeviceId)
                .HasColumnName("deviceid");

            entity.Property(e => e.AppUserId)
                .HasColumnName("appuserid");

            entity.Property(e => e.LiveControlSessionId)
                .HasColumnName("livecontrolsessionid");

            entity.Property(e => e.CommandName)
                .HasMaxLength(100)
                .HasColumnName("commandname");

            entity.Property(e => e.PayloadJson)
                .HasColumnType("jsonb")
                .HasColumnName("payloadjson");

            entity.Property(e => e.SequenceNumber)
                .HasColumnName("sequencenumber");

            entity.Property(e => e.ExpiresAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expiresatutc");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");

            entity.HasIndex(e => e.DeviceId)
                .HasDatabaseName("ix_livecontrolcommand_deviceid");
        });

        modelBuilder.Entity<LiveControlSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_livecontrolsession");

            entity.ToTable("livecontrolsession");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.DeviceId)
                .HasColumnName("deviceid");

            entity.Property(e => e.AppUserId)
                .HasColumnName("appuserid");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.Property(e => e.StopReason)
                .HasMaxLength(100)
                .HasColumnName("stopreason");

            entity.Property(e => e.IsRollback)
                .HasColumnName("isrollback");

            entity.Property(e => e.RollbackOfLiveControlSessionId)
                .HasColumnName("rollbackoflivecontrolsessionid");

            entity.Property(e => e.StartedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("startedatutc");

            entity.Property(e => e.EndedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("endedatutc");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.Property(e => e.ModifiedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modifieddate");

            entity.HasIndex(e => e.DeviceId)
                .HasDatabaseName("ix_livecontrolsession_deviceid");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("ix_livecontrolsession_status");
        });

        modelBuilder.Entity<LiveControlSegment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_livecontrolsegment");

            entity.ToTable("livecontrolsegment");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.LiveControlSessionId)
                .HasColumnName("livecontrolsessionid");

            entity.Property(e => e.CommandName)
                .HasMaxLength(100)
                .HasColumnName("commandname");

            entity.Property(e => e.PayloadJson)
                .HasColumnType("jsonb")
                .HasColumnName("payloadjson");

            entity.Property(e => e.DurationMs)
                .HasColumnName("durationms");

            entity.Property(e => e.Success)
                .HasColumnName("success");

            entity.Property(e => e.FailureCode)
                .HasMaxLength(100)
                .HasColumnName("failurecode");

            entity.Property(e => e.FailureMessage)
                .HasMaxLength(1000)
                .HasColumnName("failuremessage");

            entity.Property(e => e.StartedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("startedatutc");

            entity.Property(e => e.CompletedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("completedatutc");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.HasIndex(e => e.LiveControlSessionId)
                .HasDatabaseName("ix_livecontrolsegment_sessionid");
        });

        modelBuilder.Entity<TelemetryReading>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_telemetryreading");

            entity.ToTable("telemetryreading");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.DeviceId)
                .HasColumnName("deviceid");

            entity.Property(e => e.PayloadJson)
                .HasColumnType("jsonb")
                .HasColumnName("payloadjson");

            entity.Property(e => e.ProviderType)
                .HasMaxLength(50)
                .HasColumnName("providertype");

            entity.Property(e => e.RecordedAtUtc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("recordedatutc");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createddate");

            entity.HasIndex(e => e.DeviceId)
                .HasDatabaseName("ix_telemetryreading_deviceid");

            entity.HasIndex(e => new { e.DeviceId, e.RecordedAtUtc })
                .HasDatabaseName("ix_telemetryreading_deviceid_recordedatutc");
        });
    }
}
