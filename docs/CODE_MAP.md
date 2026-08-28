# Thyme-Me code map

Read this after `IMPLEMENTATION_PROMPT.md`. Product behavior remains governed by `docs/SDD.md`; this file identifies the code that implements each capability. Update it whenever ownership moves.

The public product, repository, and workspace name is **Thyme-Me**. Code uses the
dash-free `ThymeMe` identifier for .NET namespaces, assemblies, project directories,
and migration types; lowercase `thymeme` is used for executable, storage, and
single-instance identifiers. Public name and repository constants are governed by
`src/ThymeMe.Application/ProductIdentity.cs`; do not scatter a replacement brand
through C# code.

## Dependency direction

`ThymeMe.App` composes Presentation, Platform.Windows, and Infrastructure. Presentation calls Application use cases. Infrastructure implements persistence/filesystem ports. Platform.Windows implements Windows ports. Application coordinates use cases. Domain owns invariants and has no outward dependencies. `tests/ThymeMe.Architecture.Tests/DependencyRuleTests.cs` enforces these boundaries.

## Feature ownership

| Capability | Rules/use case | Adapter | UI/tests |
| --- | --- | --- | --- |
| Startup, window bounds, close/tray timer | `Application/Lifecycle/PlatformContracts.cs`, `Application/Settings/AppSettings.cs` | `App/App.xaml.cs`, `App/MainWindow.xaml.cs`, `Platform.Windows/Lifecycle/WindowsTrayService.cs` | packaged smoke tests |
| Responsive shell/interface scale | typed settings | `Infrastructure/Settings/AppSettingsValidator.cs` | `Presentation.WinUI/Shell/AppShell.xaml(.cs)`, `Settings/SettingsPage.xaml(.cs)` |
| Organization, defaults, and budgets | `Domain/Organization/*`, `Application/Organization/*` | `Infrastructure/Persistence/SqliteOrganizationStore.cs` | `Presentation.WinUI/Home/*`; association, budget, and SQLite tests |
| Normal/Pomodoro timing and completion | `Domain/Timing/*`, `Application/Timing/*` | `Infrastructure/Persistence/SqliteTimingStore.cs` | `Presentation.WinUI/Timing/*`; timing and SQLite tests |
| Crash recovery/completion queue | `Application/Timing/TimingCoordinator.cs` | `SqliteTimingStore.cs`, `DatabaseLifecycleService.cs` | `TransportViewModel.cs`; recovery integration test |
| Idle/display/lock/suspend | `Application/Lifecycle/InterruptionCoordinator.cs` | `Platform.Windows/Lifecycle/WindowsInterruptionSource.cs`, `WindowsNotificationService.cs` | Settings; platform smoke tests |
| Foreground app tracking | `Application/ActivityTracking/*` | `SqliteActivityStore.cs`, `Platform.Windows/ActivityTracking/*` | Settings Activity & Privacy |
| History/manual/continue/adjustments/deletion | `Application/History/*` | `Infrastructure/Persistence/SqliteHistoryStore.cs` | `Presentation.WinUI/History/*`, Home actions; SQLite tests |
| Rounding/billing | `Domain/Reporting/RoundingPolicy.cs`, `Domain/Billing/BillingPolicy.cs` | Session/Adjustment persistence rows | timing/history/stats/settings; domain tests |
| Stats ranges/views/rankings/earnings | `Application/Reporting/ReportingContracts.cs` | `Infrastructure/Persistence/SqliteReportingStore.cs` | `Presentation.WinUI/Reporting/StatsViewModel.cs`, `StatsPage.xaml(.cs)` |
| Archive/cascade deletion | `Application/Archive/*` | `Infrastructure/Persistence/SqliteArchiveStore.cs` | `Presentation.WinUI/Archive/*`; cascade integration test |
| Export/backup/restore | `Application/DataPortability/*`, `Domain/DataPortability/DefensiveLimits.cs` | `Infrastructure/DataPortability/*`, `Platform.Windows/DataPortability/WindowsFileDialogService.cs` | first-run and Settings Data |
| Settings/onboarding wizard | `Application/Settings/*` | `Infrastructure/Settings/*` | `Presentation.WinUI/Settings/*`, `Onboarding/FirstRunWizardView.xaml(.cs)`, `FirstRunWizardViewModel.cs` |
| Currency selection/conversion | `Domain/Billing/BillingPolicy.cs` | ISO code/minor-unit fields in persistence | `Presentation.WinUI/Common/CurrencyCatalog.cs`, `Home/HomePage.xaml.cs`; currency tests |
| Appearance | `Domain/Appearance/*` | settings and Palette rows | Settings and `Shell/AppShell.xaml.cs`; contrast tests |
| Updates | `Application/Updates/UpdateContracts.cs` | `Platform.Windows/Updates/*` | Settings Updates; owner configuration required |
| Product/release identity, provenance, license | `Application/ProductIdentity.cs`, `LICENSE`, `NOTICE`, `PROVENANCE.json`, `Directory.Build.props` | `.github/workflows/release.yml`, `src/ThymeMe.App/Package.appxmanifest` | `Settings/SettingsViewModel.cs`, `SettingsPage.xaml`, release evidence |
| Calendar seam | `ICalendarIntegration` | `DeferredCalendarIntegration` | deliberately no UI |
| Schema/migrations | Domain/Application models | `PersistenceRows.cs`, `ThymeMeDbContext.cs`, `Migrations/` | migration/SQLite tests |

All paths above are relative to `src/ThymeMe.*` unless already rooted at `tests/`. The compact machine index is `docs/feature-index.json`.

## Change routing

- Change an invariant in Domain and its deterministic tests first.
- Change workflows through Application services/ports; views never call EF Core or Win32/WinRT.
- Change stored shape in `PersistenceRows.cs` and `ThymeMeDbContext.cs`, then add—not rewrite—an EF migration and test.
- Keep native handles owned and disposed by Platform.Windows adapters.
- XAML owns layout; its paired view model owns presentation state; neither owns domain rules.
- Version export/backup changes and add compatibility, hostile-input, resource-limit, and rollback tests.
