using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeTrek.Application.ActivityTracking;
using TimeTrek.Application.Archive;
using TimeTrek.Application.DataPortability;
using TimeTrek.Application.History;
using TimeTrek.Application.Lifecycle;
using TimeTrek.Application.Organization;
using TimeTrek.Application.Reporting;
using TimeTrek.Application.Settings;
using TimeTrek.Application.Timing;
using TimeTrek.Infrastructure.DataPortability;
using TimeTrek.Infrastructure.Persistence;
using TimeTrek.Infrastructure.Settings;

namespace TimeTrek.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTimeTrekInfrastructure(
        this IServiceCollection services,
        string applicationDataDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDataDirectory);

        string databasePath = Path.Combine(Path.GetFullPath(applicationDataDirectory), "timetrek.db");
        services.AddPooledDbContextFactory<TimeTrekDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath};Pooling=True;Default Timeout=5"));
        services.AddHostedService<DatabaseLifecycleService>();
        services.AddSingleton<IAppSettingsStore, SqliteAppSettingsStore>();
        services.AddSingleton<IOrganizationStore, SqliteOrganizationStore>();
        services.AddSingleton<ITimingStore, SqliteTimingStore>();
        services.AddSingleton<IHistoryStore, SqliteHistoryStore>();
        services.AddSingleton<IReportingStore, SqliteReportingStore>();
        services.AddSingleton<IArchiveStore, SqliteArchiveStore>();
        services.AddSingleton<IActivityStore, SqliteActivityStore>();
        services.AddSingleton<ArchiveService>();
        services.AddSingleton<IDataPortabilityService>(provider => new SqliteDataPortabilityService(
            provider.GetRequiredService<IDbContextFactory<TimeTrekDbContext>>(),
            provider.GetRequiredService<IAppSettingsStore>(),
            applicationDataDirectory));
        services.AddSingleton<ILocalTimeContext, SystemLocalTimeContext>();
        services.AddSingleton<AppBootstrapService>();
        services.AddSingleton<OrganizationService>();
        services.AddSingleton<TimingCoordinator>();
        services.AddHostedService<TimingBackgroundService>();
        services.AddHostedService<ActivityTrackingCoordinator>();
        services.AddHostedService<InterruptionCoordinator>();
        services.AddSingleton<HistoryService>();
        return services;
    }
}
