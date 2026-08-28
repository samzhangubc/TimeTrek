using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThymeMe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdjustmentCategories",
                columns: table => new
                {
                    AdjustmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdjustmentCategories", x => new { x.AdjustmentId, x.CategoryId });
                });

            migrationBuilder.CreateTable(
                name: "Adjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StreamId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocalDateUnixDays = table.Column<long>(type: "INTEGER", nullable: false),
                    TimeZoneId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RawDurationMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    EffectiveDurationMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    RoundingIncrementMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    RoundingRule = table.Column<int>(type: "INTEGER", nullable: false),
                    IsBillable = table.Column<bool>(type: "INTEGER", nullable: false),
                    HourlyRateMinorUnits = table.Column<long>(type: "INTEGER", nullable: true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    EstimatedEarningMinorUnits = table.Column<long>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 16384, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    PurgeAfterUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    DeletionBatchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adjustments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeletionBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RootKind = table.Column<int>(type: "INTEGER", nullable: false),
                    RootId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RootName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DeletedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    PurgeAfterUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletionBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Palettes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FormatVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "INTEGER", nullable: false),
                    LightCanvas = table.Column<string>(type: "TEXT", nullable: false),
                    LightSurface = table.Column<string>(type: "TEXT", nullable: false),
                    LightAccent = table.Column<string>(type: "TEXT", nullable: false),
                    DarkCanvas = table.Column<string>(type: "TEXT", nullable: false),
                    DarkSurface = table.Column<string>(type: "TEXT", nullable: false),
                    DarkAccent = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Palettes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchemaState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    CleanShutdown = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConsecutiveStartupFailures = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemaState", x => x.Id);
                    table.CheckConstraint("CK_SchemaState_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    JsonValue = table.Column<string>(type: "TEXT", maxLength: 65536, nullable: false),
                    SchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Streams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    BudgetMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    BudgetResetPeriod = table.Column<int>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    PurgeAfterUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    DeletionBatchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Streams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimingRoots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SingletonKey = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TimingMode = table.Column<int>(type: "INTEGER", nullable: false),
                    StreamId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CategoryIdsJson = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    IsBillable = table.Column<bool>(type: "INTEGER", nullable: false),
                    HourlyRateMinorUnits = table.Column<long>(type: "INTEGER", nullable: true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    RoundingIncrementMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    RoundingRule = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    CurrentSegmentStartedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    RequestedDurationMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    ScheduledEndUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    AccumulatedWorkMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    AccumulatedBreakMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    PomodoroTotalMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    PomodoroWorkMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    PomodoroBreakMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    PomodoroBufferMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    PomodoroBreaksBillable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimingRoots", x => x.Id);
                    table.CheckConstraint("CK_TimingRoots_Singleton", "SingletonKey = 1");
                });

            migrationBuilder.CreateTable(
                name: "TimingSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    StartUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    EndUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimingSegments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StreamId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DefaultBillable = table.Column<bool>(type: "INTEGER", nullable: false),
                    HourlyRateMinorUnits = table.Column<long>(type: "INTEGER", nullable: true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    PurgeAfterUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    DeletionBatchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Streams_StreamId",
                        column: x => x.StreamId,
                        principalTable: "Streams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StreamId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BudgetMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    BudgetResetPeriod = table.Column<int>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    PurgeAfterUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    DeletionBatchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Streams_StreamId",
                        column: x => x.StreamId,
                        principalTable: "Streams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Origin = table.Column<int>(type: "INTEGER", nullable: false),
                    TimingMode = table.Column<int>(type: "INTEGER", nullable: false),
                    StreamId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StartUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    EndUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    TimeZoneId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartUtcOffsetMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    EndUtcOffsetMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    RawDurationMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    EffectiveDurationMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    RoundingIncrementMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    RoundingRule = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedDurationMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 16384, nullable: true),
                    CompletionPending = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsBillable = table.Column<bool>(type: "INTEGER", nullable: false),
                    HourlyRateMinorUnits = table.Column<long>(type: "INTEGER", nullable: true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    EstimatedEarningMinorUnits = table.Column<long>(type: "INTEGER", nullable: true),
                    StreamOriginalName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ProjectOriginalName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    PurgeAfterUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    DeletionBatchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedUtcMilliseconds = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Streams_StreamId",
                        column: x => x.StreamId,
                        principalTable: "Streams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ForegroundApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ExecutableFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForegroundApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForegroundApplications_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionCategories",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionCategories", x => new { x.SessionId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_SessionCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionCategories_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adjustments_IsDeleted_LocalDateUnixDays",
                table: "Adjustments",
                columns: new[] { "IsDeleted", "LocalDateUnixDays" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_StreamId_IsDeleted_IsArchived",
                table: "Categories",
                columns: new[] { "StreamId", "IsDeleted", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_DeletionBatches_PurgeAfterUtcMilliseconds",
                table: "DeletionBatches",
                column: "PurgeAfterUtcMilliseconds");

            migrationBuilder.CreateIndex(
                name: "IX_ForegroundApplications_SessionId_ExecutableFileName",
                table: "ForegroundApplications",
                columns: new[] { "SessionId", "ExecutableFileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_StreamId_IsDeleted_IsArchived",
                table: "Projects",
                columns: new[] { "StreamId", "IsDeleted", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionCategories_CategoryId_SessionId",
                table: "SessionCategories",
                columns: new[] { "CategoryId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_IsDeleted_StartUtcMilliseconds",
                table: "Sessions",
                columns: new[] { "IsDeleted", "StartUtcMilliseconds" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ProjectId_StartUtcMilliseconds",
                table: "Sessions",
                columns: new[] { "ProjectId", "StartUtcMilliseconds" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_StreamId_StartUtcMilliseconds",
                table: "Sessions",
                columns: new[] { "StreamId", "StartUtcMilliseconds" });

            migrationBuilder.CreateIndex(
                name: "IX_Streams_IsDeleted_IsArchived_SortOrder",
                table: "Streams",
                columns: new[] { "IsDeleted", "IsArchived", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TimingRoots_SingletonKey",
                table: "TimingRoots",
                column: "SingletonKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimingSegments_SessionId_StartUtcMilliseconds",
                table: "TimingSegments",
                columns: new[] { "SessionId", "StartUtcMilliseconds" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdjustmentCategories");

            migrationBuilder.DropTable(
                name: "Adjustments");

            migrationBuilder.DropTable(
                name: "DeletionBatches");

            migrationBuilder.DropTable(
                name: "ForegroundApplications");

            migrationBuilder.DropTable(
                name: "Palettes");

            migrationBuilder.DropTable(
                name: "SchemaState");

            migrationBuilder.DropTable(
                name: "SessionCategories");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "TimingRoots");

            migrationBuilder.DropTable(
                name: "TimingSegments");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Streams");
        }
    }
}
