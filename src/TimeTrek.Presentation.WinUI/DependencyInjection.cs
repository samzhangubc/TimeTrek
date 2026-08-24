using Microsoft.Extensions.DependencyInjection;
using TimeTrek.Presentation.WinUI.Archive;
using TimeTrek.Presentation.WinUI.History;
using TimeTrek.Presentation.WinUI.Home;
using TimeTrek.Presentation.WinUI.Onboarding;
using TimeTrek.Presentation.WinUI.Reporting;
using TimeTrek.Presentation.WinUI.Settings;
using TimeTrek.Presentation.WinUI.Shell;
using TimeTrek.Presentation.WinUI.Timing;

namespace TimeTrek.Presentation.WinUI;

public static class DependencyInjection
{
    public static IServiceCollection AddTimeTrekPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<StatsViewModel>();
        services.AddSingleton<ArchiveViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<TransportViewModel>();
        services.AddSingleton<FirstRunWizardViewModel>();
        services.AddSingleton<AppShellViewModel>();
        return services;
    }
}
