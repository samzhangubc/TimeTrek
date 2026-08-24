using Microsoft.EntityFrameworkCore;
using TimeTrek.Application.Archive;
using TimeTrek.Application.History;
using TimeTrek.Application.Organization;
using TimeTrek.Application.Reporting;
using TimeTrek.Application.Timing;
using TimeTrek.Domain.Organization;
using TimeTrek.Domain.Reporting;
using TimeTrek.Domain.Timing;
using TimeTrek.Infrastructure.Persistence;

namespace TimeTrek.Infrastructure.Tests.Persistence;

public sealed class SqlitePersistenceTests
{
    [Fact]
    public async Task InitialMigrationsApplyToEmptyDatabase()
    {
        await using SqliteTestDatabase database = new();

        await database.InitializeAsync();
        await using TimeTrekDbContext context = database.CreateDbContext();
        string[] applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();

        Assert.Contains(applied, migration => migration.EndsWith("_InitialSchema", StringComparison.Ordinal));
        Assert.Contains(applied, migration => migration.EndsWith("_DurableSessionDrafts", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OrganizationAndTimingRoundTripThroughRealSqlite()
    {
        await using SqliteTestDatabase database = new();
        await database.InitializeAsync();
        MutableTimeProvider time = new(DateTimeOffset.Parse("2026-08-23T12:00:00Z", null));
        SqliteOrganizationStore organizationStore = new(database);
        OrganizationService organizations = new(organizationStore, time);
        SqliteTimingStore timingStore = new(database);
        TimingCoordinator coordinator = new(timingStore, organizationStore, new FixedLocalTimeContext(), time);
        SqliteHistoryStore history = new(database);

        StreamDefinition stream = (await organizations.CreateStreamAsync("MATH 100")).Value!;
        CategoryDefinition category = (await organizations.CreateCategoryAsync("Lecture", stream.Id, false, null, null)).Value!;
        StartTimingRequest request = new(
            "25",
            TimingMode.Normal,
            SessionAssociations.Create(stream.Id, [category.Id], null),
            false,
            null,
            null,
            null,
            RoundingRule.None,
            null);

        Assert.True((await coordinator.StartAsync(request)).IsSuccess);
        time.Advance(TimeSpan.FromMinutes(10));
        Assert.True((await coordinator.PauseAsync()).IsSuccess);
        time.Advance(TimeSpan.FromMinutes(5));
        Assert.True((await coordinator.ResumeAsync()).IsSuccess);
        time.Advance(TimeSpan.FromMinutes(5));
        Assert.True((await coordinator.StopAsync()).IsSuccess);

        HistoryPage page = await history.QueryAsync(new HistoryQuery());
        Assert.Single(page.Items);
        Assert.Equal(15 * 60_000, page.Items[0].RawDurationMilliseconds);
        Assert.Equal("MATH 100", page.Items[0].StreamName);
        Assert.Equal(["Lecture"], page.Items[0].CategoryNames);
    }

    [Fact]
    public async Task UniqueTimingRootRejectsSecondActiveSession()
    {
        await using SqliteTestDatabase database = new();
        await database.InitializeAsync();
        MutableTimeProvider time = new(DateTimeOffset.Parse("2026-08-23T12:00:00Z", null));
        SqliteOrganizationStore organizations = new(database);
        TimingCoordinator coordinator = new(
            new SqliteTimingStore(database),
            organizations,
            new FixedLocalTimeContext(),
            time);
        StartTimingRequest request = new(
            "25",
            TimingMode.Normal,
            SessionAssociations.Empty,
            false,
            null,
            null,
            null,
            RoundingRule.None,
            null);

        Assert.True((await coordinator.StartAsync(request)).IsSuccess);
        Assert.False((await coordinator.StartAsync(request)).IsSuccess);
    }

    [Fact]
    public async Task StopWithoutActiveSessionDoesNotCreateHistory()
    {
        await using SqliteTestDatabase database = new();
        await database.InitializeAsync();
        TimingCoordinator coordinator = new(
            new SqliteTimingStore(database),
            new SqliteOrganizationStore(database),
            new FixedLocalTimeContext(),
            new MutableTimeProvider(DateTimeOffset.Parse("2026-08-23T12:00:00Z", null)));

        var result = await coordinator.StopAsync();
        HistoryPage page = await new SqliteHistoryStore(database).QueryAsync(new HistoryQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("timing.inactive", result.Error?.Code);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task StreamAndProjectBudgetsPersistThroughUpdates()
    {
        await using SqliteTestDatabase database = new();
        await database.InitializeAsync();
        MutableTimeProvider time = new(DateTimeOffset.Parse("2026-08-23T12:00:00Z", null));
        SqliteOrganizationStore store = new(database);
        OrganizationService organizations = new(store, time);
        StreamDefinition stream = (await organizations.CreateStreamAsync("Research")).Value!;
        ProjectDefinition project = (await organizations.CreateProjectAsync("Paper", stream.Id)).Value!;

        time.Advance(TimeSpan.FromMinutes(1));
        Assert.True((await organizations.UpdateStreamAsync(
            stream.Id,
            "Research and writing",
            "#336699",
            new TimeBudget(12 * 60 * 60_000, BudgetResetPeriod.Weekly))).IsSuccess);
        Assert.True((await organizations.UpdateProjectAsync(
            project.Id,
            "Paper draft",
            stream.Id,
            new TimeBudget(20 * 60 * 60_000, BudgetResetPeriod.Monthly))).IsSuccess);

        StreamDefinition savedStream = Assert.Single(await store.ListStreamsAsync(false));
        ProjectDefinition savedProject = Assert.Single(await store.ListProjectsAsync(false));
        Assert.Equal("Research and writing", savedStream.Name);
        Assert.Equal(new TimeBudget(12 * 60 * 60_000, BudgetResetPeriod.Weekly), savedStream.Budget);
        Assert.Equal("Paper draft", savedProject.Name);
        Assert.Equal(new TimeBudget(20 * 60 * 60_000, BudgetResetPeriod.Monthly), savedProject.Budget);
    }

    [Fact]
    public async Task ExpiredTimingRootRecoversExactlyOnceIntoPendingCompletion()
    {
        await using SqliteTestDatabase database = new();
        await database.InitializeAsync();
        MutableTimeProvider time = new(DateTimeOffset.Parse("2026-08-23T12:00:00Z", null));
        SqliteTimingStore timingStore = new(database);
        TimingCoordinator coordinator = new(
            timingStore,
            new SqliteOrganizationStore(database),
            new FixedLocalTimeContext(),
            time);
        StartTimingRequest request = new(
            "1", TimingMode.Normal, SessionAssociations.Empty, false, null, null, null, RoundingRule.None, null);

        Assert.True((await coordinator.StartAsync(request)).IsSuccess);
        time.Advance(TimeSpan.FromMinutes(2));
        Assert.True((await coordinator.RecoverAsync()).IsSuccess);

        Assert.Null(await coordinator.GetActiveAsync());
        Assert.Single(await coordinator.ListPendingCompletionsAsync());
        Assert.False((await coordinator.RecoverAsync()).IsSuccess);
        Assert.Single(await coordinator.ListPendingCompletionsAsync());
    }

    [Fact]
    public async Task OrganizationCascadeDeleteAndRestoreIsAtomic()
    {
        await using SqliteTestDatabase database = new();
        await database.InitializeAsync();
        MutableTimeProvider time = new(DateTimeOffset.Parse("2026-08-23T12:00:00Z", null));
        SqliteOrganizationStore organizationStore = new(database);
        OrganizationService organizations = new(organizationStore, time);
        StreamDefinition stream = (await organizations.CreateStreamAsync("Client")).Value!;
        _ = await organizations.CreateCategoryAsync("Meetings", stream.Id, false, null, null);
        ArchiveService archive = new(new SqliteArchiveStore(database), time);

        DeletionPreview preview = await archive.PreviewDeletionAsync(OrganizationKind.Stream, stream.Id);
        Assert.Equal(2, preview.OrganizationCount);
        Guid batchId = await archive.DeleteCascadeAsync(preview, null, 30);

        Assert.Empty(await organizationStore.ListStreamsAsync(false));
        await new SqliteArchiveStore(database).RestoreBatchAsync(batchId);
        Assert.Single(await organizationStore.ListStreamsAsync(false));
        Assert.Single(await organizationStore.ListCategoriesAsync(false));
    }

    [Fact]
    public async Task NegativeAdjustmentAppearsInHistoryAndSignedStats()
    {
        await using SqliteTestDatabase database = new();
        await database.InitializeAsync();
        MutableTimeProvider time = new(DateTimeOffset.Parse("2026-08-23T12:00:00Z", null));
        SqliteOrganizationStore organizations = new(database);
        SqliteHistoryStore historyStore = new(database);
        HistoryService history = new(historyStore, organizations, new FixedLocalTimeContext(), time);

        Assert.True((await history.AddAdjustmentAsync(new AddAdjustmentRequest(
            new DateOnly(2026, 8, 23).DayNumber - new DateOnly(1970, 1, 1).DayNumber,
            -30 * 60_000,
            SessionAssociations.Empty,
            false,
            null,
            null,
            null,
            RoundingRule.None,
            "Correction"))).IsSuccess);

        HistoryPage page = await historyStore.QueryAsync(new HistoryQuery());
        HistoryItem item = Assert.Single(page.Items);
        Assert.Equal(SessionOrigin.Adjustment, item.Origin);
        Assert.Equal(-30 * 60_000, page.SignedTotalDurationMilliseconds);

        StatsSnapshot stats = await new SqliteReportingStore(database).GetStatsAsync(new ReportingQuery(
            DateTimeOffset.Parse("2026-08-23T00:00:00Z", null).ToUnixTimeMilliseconds(),
            DateTimeOffset.Parse("2026-08-24T00:00:00Z", null).ToUnixTimeMilliseconds(),
            ReportingBasis.Raw));
        Assert.Equal(-30 * 60_000, stats.SelectedRangeMilliseconds);
        Assert.Equal(-30 * 60_000, stats.LifetimeMilliseconds);
    }

    private sealed class FixedLocalTimeContext : ILocalTimeContext
    {
        public string TimeZoneId => "UTC";

        public int GetUtcOffsetMinutes(long utcMilliseconds) => 0;
    }
}
