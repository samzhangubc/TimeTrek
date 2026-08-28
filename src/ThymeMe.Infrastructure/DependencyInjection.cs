using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ThymeMe.Application.ActivityTracking;
using ThymeMe.Application.Archive;
using ThymeMe.Application.DataPortability;
using ThymeMe.Application.History;
using ThymeMe.Application.Lifecycle;
using ThymeMe.Application.Organization;
using ThymeMe.Application.Reporting;
using ThymeMe.Application.Settings;
using ThymeMe.Application.Timing;
using ThymeMe.Infrastructure.DataPortability;
using ThymeMe.Infrastructure.Persistence;
using ThymeMe.Infrastructure.Settings;

namespace ThymeMe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddThymeMeInfrastructure(
        this IServiceCollection services,
        string applicationDataDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDataDirectory);

        string databasePath = Path.Combine(Path.GetFullPath(applicationDataDirectory), "thymeme.db");
        services.AddPooledDbContextFactory<ThymeMeDbContext>(options =>
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
            provider.GetRequiredService<IDbContextFactory<ThymeMeDbContext>>(),
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
