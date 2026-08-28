using Microsoft.Extensions.DependencyInjection;
using ThymeMe.Application.DataPortability;
using ThymeMe.Application.Lifecycle;
using ThymeMe.Application.Updates;
using ThymeMe.Platform.Windows.ActivityTracking;
using ThymeMe.Platform.Windows.DataPortability;
using ThymeMe.Platform.Windows.Lifecycle;
using ThymeMe.Platform.Windows.Updates;

namespace ThymeMe.Platform.Windows;

public static class DependencyInjection
{
    public static IServiceCollection AddThymeMeWindowsPlatform(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<INotificationService, WindowsNotificationService>();
        services.AddSingleton<ITrayService, WindowsTrayService>();
        services.AddSingleton<IInterruptionSource, WindowsInterruptionSource>();
        services.AddSingleton<IForegroundApplicationSource, WindowsForegroundApplicationSource>();
        services.AddSingleton<IStartupRegistrationService, WindowsStartupRegistrationService>();
        services.AddSingleton<IClipboardService, WindowsClipboardService>();
        services.AddSingleton<IUpdateService, UnconfiguredUpdateService>();
        services.AddSingleton<IUserFileDialogService, WindowsFileDialogService>();
        services.AddSingleton<ICalendarIntegration, DeferredCalendarIntegration>();
        return services;
    }
}
