using Microsoft.Extensions.DependencyInjection;
using TimeTrek.Application.DataPortability;
using TimeTrek.Application.Lifecycle;
using TimeTrek.Application.Updates;
using TimeTrek.Platform.Windows.ActivityTracking;
using TimeTrek.Platform.Windows.DataPortability;
using TimeTrek.Platform.Windows.Lifecycle;
using TimeTrek.Platform.Windows.Updates;

namespace TimeTrek.Platform.Windows;

public static class DependencyInjection
{
    public static IServiceCollection AddTimeTrekWindowsPlatform(this IServiceCollection services)
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
