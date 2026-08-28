using Microsoft.Extensions.DependencyInjection;
using ThymeMe.Presentation.WinUI.Archive;
using ThymeMe.Presentation.WinUI.History;
using ThymeMe.Presentation.WinUI.Home;
using ThymeMe.Presentation.WinUI.Onboarding;
using ThymeMe.Presentation.WinUI.Reporting;
using ThymeMe.Presentation.WinUI.Settings;
using ThymeMe.Presentation.WinUI.Shell;
using ThymeMe.Presentation.WinUI.Timing;

namespace ThymeMe.Presentation.WinUI;

public static class DependencyInjection
{
    public static IServiceCollection AddThymeMePresentation(this IServiceCollection services)
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
