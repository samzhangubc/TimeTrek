# TimeTrek Technical Design

| Field | Value |
| --- | --- |
| Status | Approved implementation baseline |
| Approved | 2026-08-23 |
| Product authority | `docs/SDD.md` |
| Interface authority | `docs/INTERFACE.md` |

## 1. Purpose and decision order

This document fixes the implementation methods that were intentionally left open while the product behavior was being designed. It exists to keep TimeTrek modular, testable, and practical for one maintainer to patch over many releases.

When documents appear to conflict, use this order:

1. `docs/SDD.md` for user-visible behavior and invariants.
2. `docs/INTERFACE.md` and `docs/FIRST_RUN_WIZARD.md` for interaction and layout.
3. This document for architecture and implementation methods.
4. `docs/CONTEXT.md` and `llm.txt` for condensed rationale and guardrails.

Implementation may proceed in verified increments, but every non-Calendar requirement remains part of the single initial-release baseline. An incremental build order is not permission to discard or silently defer a requirement.

## 2. Selected stack

- **Language/runtime:** C# 14 on .NET 10 LTS, using the latest supported .NET 10 servicing release available to the build.
- **Desktop UI:** WinUI 3 on the latest compatible stable Windows App SDK. Preview or experimental SDK channels are prohibited in release builds.
- **Application model:** WinUI desktop application that can run unpackaged; MSIX project support remains for development and a future signed installer.
- **Distribution:** an intentionally unsigned, self-contained x64 portable ZIP distributed through GitHub Releases. ARM64 and x86 packages are not part of the 1.0.1 release.
- **Minimum target:** the package remains technically installable from Windows 10 version 1809, build 17763, so Windows 10 22H2 can be compatibility-tested. Windows 11 x64 is the supported platform without an SLA. TimeTrek does not use APIs newer than the declared minimum without capability checks and a working fallback.
- **Hosting/composition:** .NET Generic Host and the built-in dependency-injection, configuration, and logging abstractions.
- **Presentation pattern:** MVVM. `CommunityToolkit.Mvvm` may provide observable-property and command plumbing; it must not contain domain behavior.
- **Persistence:** SQLite through the stable .NET 10-compatible EF Core SQLite provider.
- **Testing:** the .NET test platform with one consistently used test framework selected during scaffolding. Tests must run from `dotnet test` without requiring the installed application except for explicitly labelled packaged-platform tests.

All package versions are pinned centrally. Stable patch updates are allowed through reviewed dependency-update commits; automatic unreviewed major upgrades are not.

## 3. Architectural style

TimeTrek is a **modular monolith**. It has one main application process and one local database. It does not introduce an internal HTTP server, hosted backend, Windows service, microservices, or a general-purpose plugin system.

The solution uses these responsibilities:

- **`TimeTrek.Domain`** — entities, value objects, policies, state machines, validation, and domain errors. It references no UI, EF Core, filesystem, network, or Windows API package.
- **`TimeTrek.Application`** — commands, queries, use-case orchestration, authorization-free local workflows, transaction boundaries, ports, and result models. It references Domain.
- **`TimeTrek.Infrastructure`** — EF Core mappings and migrations, repositories/query implementations, settings, export, backup/restore, rollback snapshots, and privacy-safe local logging. It implements Application ports.
- **`TimeTrek.Platform.Windows`** — notification-area behavior, app notifications, single instancing, activation, startup registration, foreground-app observation, input inactivity, power/display/session events, wake prevention, protected credentials, and updates. It implements Application ports.
- **`TimeTrek.Presentation.WinUI`** — pages, dialogs, controls, view models, navigation, accessibility metadata, formatting, and visual state. It invokes Application use cases and does not access EF Core or Win32 directly.
- **`TimeTrek.App`** — executable, package manifest, Generic Host setup, dependency registrations, startup sequencing, activation routing, and graceful shutdown. It is the composition root and is the only production project permitted to reference every implementation assembly.
- **Test projects** — unit tests for Domain/Application, SQLite integration and migration tests, adapter contract tests, UI/view-model tests, and packaged Windows smoke tests.

Feature folders inside each assembly should use the same vocabulary—Timing, Organization, History, Reporting, Billing, Archive, Appearance, Settings, DataPortability, ActivityTracking, Lifecycle, and Updates—so a change can be followed vertically without creating a separate assembly for every feature.

### Dependency rules

- Domain has no outward dependency.
- Application depends only on Domain and framework abstractions.
- Infrastructure, Platform.Windows, and Presentation implement or consume Application contracts.
- Presentation must never query the database or call Win32/WinRT platform APIs directly.
- Infrastructure and Platform.Windows must never depend on WinUI views or view models.
- Domain entities are not EF Core entities and are not serialized directly for exports or backups.
- Interfaces are required at external, platform, persistence, time, filesystem, update, and other volatile boundaries. Do not create an interface mechanically for every class.
- Cross-feature behavior is coordinated through explicit Application use cases or domain policies, not static service locators or view-model-to-view-model calls.

Architecture rules shall be enforced by automated dependency tests where practical.

## 4. Domain and use-case design

Domain code contains the rules that must remain correct regardless of UI or storage:

- Stream, Category, Project, Session, adjustment, budget, billing, rounding, archive, and deletion invariants.
- Valid association combinations and previewed organization migrations/replacements.
- Duration parsing and normal/Pomodoro timing transitions.
- Raw versus rounded/effective duration and historical wage preservation.
- Archive, deletion-batch, retention, and atomic restore rules.
- Backup merge identity remapping rules.

Application commands perform mutations such as Start, Pause, Resume, Stop, Complete, Continue, AddManualSession, ApplyAdjustment, Archive, Restore, Delete, Purge, RestoreBackup, and ChangeSetting. Queries return dedicated immutable read models for Home, History, Stats, Timeline, Archive, and Settings.

Commands return typed outcomes and domain validation failures. Expected user errors are not exceptions. Exceptions are reserved for unexpected infrastructure or programming failures and are translated into safe user-facing errors at the application boundary.

## 5. Time and active-Session engine

Exactly one `TimingCoordinator` owns active timing. It is an in-process hosted component, not a Windows service. The process remains alive without its main window only for the lifecycle states allowed by the SDD.

The timing engine is an explicit state machine with serialized commands. At minimum it represents inactive, active work, manually paused, Pomodoro break-pending, Pomodoro break, work-start-pending, automatically stopped, and completion-pending states. Invalid transitions fail without mutating persistence.

- Inject .NET `TimeProvider` through an application clock port for deterministic tests.
- Persist UTC wall-clock timestamps as canonical history. Use monotonic elapsed time while the process is running to resist wall-clock jumps, then reconcile to persisted timestamps at defined lifecycle transitions.
- Persist every state transition atomically before presenting it as successful in the UI.
- Store the requested duration and scheduled automatic end. Editing duration issues a timing command; the view never rewrites timer state itself.
- A tracked Session draft is created by the successful Start transaction. Pre-start Cancel creates nothing.
- Pauses and Pomodoro segments are explicit persisted records. Raw duration is derived from work segments and materialized on completion for efficient reads, with invariant checks against the segments.
- Enforce one active/pending timing root with both application locking and a database constraint.
- On startup, recovery reads persisted state and applies the SDD's scheduled-end rule. It never guesses from a stale UI tick.
- Foreground-application tracking subscribes to the timing state. It cannot begin, continue, or collect during pauses or Pomodoro breaks independently of that state.

All automatic-end, pause, recovery, DST, time-zone, suspend, display-off, and clock-change behavior requires fake-clock unit tests plus packaged Windows tests where platform events are involved.

## 6. Persistence and physical representation

SQLite is the single local source of truth. Use one EF Core `DbContext` per application unit of work; do not retain a context in view models or long-lived services. Mutations use transactions. Enable foreign keys, a bounded busy timeout, and WAL after verifying packaged-file behavior.

The initial schema contains versioned equivalents of:

- Streams, Categories, Projects, Sessions, SessionCategories, timing/Pomodoro segments, negative adjustments and their Category joins.
- Foreground-application aggregates linked to their exact Session.
- Time budgets, appearance palettes, typed settings, deletion batches, and schema metadata.
- Persisted active/completion state sufficient for deterministic crash recovery.

Representation rules:

- New stable identifiers use UUID version 7 and export as canonical strings.
- UTC instants use signed Unix milliseconds. Preserve the Windows time-zone identifier and the relevant UTC offsets needed to reconstruct displayed local context.
- Durations use signed 64-bit milliseconds internally. Exported seconds are derived without discarding the stored raw value.
- Monetary amounts use signed integer currency-minor units plus an ISO 4217 currency code; never use binary floating point for money.
- Session-to-Category is an explicit join with no ordering semantics.
- Archive state is separate from deletion state.
- Recoverable deletions remain in their original tables with deletion timestamp, purge deadline, and deletion-batch identifier. Organization cascade restoration is one transaction over the complete batch.
- Foreground records are accumulated per Session and application identity rather than storing window titles or content. Short event intervals may be folded into the aggregate transactionally.
- Settings use typed application accessors over a versioned persisted representation. Unknown setting keys survive backup/restore but are never trusted without validation.

Do not expose `IQueryable` outside Infrastructure. Reporting queries may use compiled EF queries or reviewed parameterized SQL behind query interfaces when measurement shows that ordinary LINQ translation is insufficient.

Derived reports initially query canonical records. Add cached or materialized aggregates only after benchmarks demonstrate a need, and then include rebuild and consistency verification paths.

## 7. Migrations, writes, and recovery

- Every released schema has an explicit monotonically increasing version and committed migration.
- Before applying a release migration, create and validate a local rollback copy using SQLite's supported backup mechanism.
- Migrations are forward-only in production, transactional where SQLite permits, repeatable in tests, and verified against fixtures for every supported previous release schema.
- Startup never opens the normal UI against a partially migrated database.
- A migration failure leaves the previous database recoverable and opens a focused recovery path; it does not silently create an empty replacement.
- Multi-object archive, delete, restore, import, and backup-merge operations are atomic.
- Settings that apply immediately are committed before showing **Saved**.
- File outputs use a temporary file in the destination directory followed by atomic replacement where supported.

The database and logs reside under the appropriate per-user packaged application-data location. User-selected exports and backups are the only routine writes outside app data.

## 8. Export, backup, and restore seams

CSV, JSON, and backup schemas use dedicated versioned transfer models. They do not serialize EF entities or view models.

- Export transformations consume a read-only snapshot and produce deterministic output for a given schema version.
- CSV field/property ordering is frozen with the first implementation and covered by golden-file tests.
- Backup ZIP creation streams versioned JSON entries, checksums, settings, palettes, activity aggregates, and the data dictionary defined by the SDD.
- Restore parsing, validation, ID remapping, and preview occur without mutating the live store.
- Replace and Merge execute only after confirmation and rollback-backup creation.
- Restore and export adapters accept streams rather than requiring all records in memory.
- Credentials and future OAuth tokens live behind a protected-credential port and are never included in exports or backups.

## 9. Windows platform adapters

Each Windows capability has a small adapter and a platform-neutral contract. Adapters publish normalized events to Application; they do not mutate domain objects directly.

- **Single instance/activation:** Windows App SDK `AppInstance` redirection.
- **App notifications:** Windows App SDK app-notification APIs, with activation routed through the composition root.
- **Notification area:** a narrowly wrapped Win32 notification-icon adapter, including dynamic timer icon/tooltip behavior and cleanup.
- **Foreground application:** an out-of-context foreground-event hook with low-frequency reconciliation, process identity lookup without elevation, and explicit Unknown-elevated aggregation.
- **Inactivity:** `GetLastInputInfo` behind an idle-source contract.
- **Power/display/session:** Windows App SDK power notifications plus the minimum Win32 session/power notifications needed to distinguish the SDD's events.
- **Wake prevention:** scoped acquisition/release around the eligible active foreground state; cleanup is mandatory on every stop, hide/exit, exception, and shutdown path.
- **Screen saver:** capability-tested detection in the Windows adapter. Failure to support it reliably must be visible in tests and must not be disguised as successful prevention.
- **Startup:** packaged per-user startup registration, off by default.
- **Protected credentials:** Windows credential protection used only if a future integration introduces secrets.

Platform APIs are capability checked. Unsupported or inaccessible data becomes a documented degraded state, never an elevation prompt.

## 10. Presentation and accessibility

Views and controls contain layout and visual behavior only. View models expose immutable screen state and invoke Application commands/queries. Long-running work is cancellable and never blocks the UI thread.

- Use WinUI virtualization primitives for History, Archive, and large Stream collections.
- Implement heatmap, timeline, budget bars, and simple pie visualizations as TimeTrek-owned WinUI controls unless a dependency review establishes a clear accessibility and maintenance advantage for a library.
- Every custom control exposes Windows UI Automation name, role, value/state, keyboard interaction, focus visuals, and non-color cues.
- Resizing, icon scale, filters, columns, and other persisted layout state go through typed settings services.
- Formatting of duration, money, dates, time zones, and regional input is centralized and separately tested.
- View models never use timers as time truth; display refreshes read a timing snapshot from Application.

Exact remaining copy, icon glyphs, and palette triplets are visual assets/polish rather than architecture changes and may be finalized during implementation without altering confirmed behavior.

## 11. Updates and release boundary

Update behavior is exposed through an `IUpdateService`; no page or view model calls GitHub or package-deployment APIs directly.

- Git tags and stable GitHub Releases remain the release authority.
- Version 1.0.1 uses manual updates. The inactive update adapter reports that automatic updating is not configured instead of claiming the current version is up to date.
- The release archive is intentionally unsigned. The workflow publishes a SHA-256 checksum, SPDX SBOM, and GitHub/Sigstore provenance and SBOM attestations; the user-facing release plainly warns about Unknown publisher and SmartScreen.
- A future update adapter may check public release metadata over HTTPS, ignore prereleases, compare semantic versions, and select the matching x64 portable artifact.
- Before any future automatic download or replacement, verify the published SHA-256 checksum and GitHub artifact attestation. Never replace executable files in process, and defer update actions while timing or completion state is active/pending.
- The update subsystem remains behind its application interface so a later signed installer or portable updater can replace the inactive implementation without changing presentation or domain code.

No private signing key, certificate password, production secret, or hidden ownership fingerprint enters source control. A future signed installer requires a separately approved package identity, trusted signing method, and update design.

## 12. Diagnostics, privacy, and security

- No telemetry, remote crash reporting, or automatic log upload.
- Use structured local logs through `ILogger`, with a bounded rolling store. Default retention is seven days and 20 MB total; oldest logs are removed first.
- Logs use event identifiers and redaction by construction. They exclude Session descriptions, tracked application names/paths, user paths, organization names, export/backup contents, secrets, and OAuth material.
- **Copy Diagnostics** uses a dedicated allowlist and does not copy ordinary log contents unless a future owner-approved workflow explicitly adds a reviewed redacted attachment.
- Treat every user string, imported file, backup, palette, release response, URI, filename, and future integration payload as untrusted input, even when it originated on the same computer.
- Parameterize every SQL query. No user input is concatenated into SQL, commands, scripts, XAML, markup, format strings, paths, or regular expressions.
- Render names, descriptions, application identities, and imported text as plain text. Do not evaluate expressions or create controls/markup from stored text.
- Use Windows APIs directly instead of invoking a command shell. If a process launch is unavoidable, use a fixed executable/verb, a structured argument API, and an explicit allowlist; never compose a shell command from user data.
- Validate text length, Unicode validity, numeric range, date/time range, identifier format, relationship counts, JSON depth, and collection size before allocation or persistence. Use checked arithmetic for durations, counts, offsets, money, archive sizes, and progress calculations.
- Parameterize literal search. Any regex introduced later must escape user literals or have an explicit short timeout and bounded input.
- Validate import/backup size, paths, ZIP entry names, checksums, schema versions, record counts, and relationships before mutation.
- Prevent ZIP path traversal, duplicate/conflicting entries, absolute/device paths, symbolic-link surprises, and decompression bombs with entry-count, expanded-size, nesting, and compression-ratio limits.
- Spreadsheet-facing CSV text cells that could execute as formulas must be reversibly neutralized. The packaged data dictionary identifies the encoding, and JSON/backup representations retain the exact original string.
- Network responses use HTTPS, bounded timeouts, bounded redirects, response-size limits, and an expected-host allowlist. Future update assets additionally require checksum and GitHub-attestation verification.
- Dependencies are centrally pinned, reviewed for license/security compatibility, and scanned in CI.
- Never request administrator elevation.

## 13. Crash, power-loss, storage-failure, and memory protection

Durable state, not an exception handler, is the primary crash-recovery mechanism.

- The application has top-level UI-thread, task, and hosted-service exception observation. Expected failures are handled at their owning boundary. An unexpected fatal exception is logged with redacted context, receives only a bounded best-effort emergency checkpoint, and terminates rather than continuing in an unknown state.
- Never rely on catching `OutOfMemoryException`, stack overflow, process corruption, forced termination, Windows bug check, or sudden power loss. The last committed transition must be sufficient for deterministic next-launch recovery.
- A successful Start is not shown until its transaction, scheduled end, timing mode, associations, and first segment are durable. Every later timing transition follows the same commit-before-success rule.
- Record a clean-shutdown marker. After an unclean shutdown, run bounded SQLite integrity checks before ordinary writes, recover the active/completion state according to the SDD, and keep the pre-recovery store available until recovery succeeds.
- Use SQLite foreign keys, WAL, and a durability level appropriate for surviving OS/power interruption; confirm the chosen PRAGMAs in packaged integration and forced-termination tests.
- Never replace a corrupt or unreadable database with an empty one. Preserve it, offer rollback/restore or read-only salvage/export when feasible, and report the exact recovery location without exposing it in copied diagnostics.
- Disk-full, access-denied, device removal, and failed-fsync/replace errors do not produce a false **Saved** state. Keep the already-durable timer root recoverable, show a focused error, and permit bounded retry or safe exit.
- Track consecutive startup failures before Home becomes stable. Repeated failures enter a safe recovery mode that suppresses optional adapters and appearance customizations, avoids automatic migrations, and exposes diagnostics/backup/restore without deleting data.
- Long operations accept cancellation, report progress, and stream or page data. Export, backup, restore validation, import, reporting, History, Archive, and activity summaries must not load an unbounded dataset into memory.
- UI collections are paged or virtualized. Caches have explicit entry/byte limits and eviction. File/JSON/ZIP processing rejects inputs that exceed documented limits before large allocation.
- Resource guards fail with a clear recoverable error and leave live data unchanged. Do not attempt to continue after a true process-wide memory exhaustion.
- Background queues have bounded capacity and defined coalescing/backpressure. Foreground-application events may aggregate safely but must never grow an unbounded in-memory queue.
- Cancellation, shutdown, and exception paths release wake requests, notification icons, hooks, file handles, database contexts, and temporary files in deterministic cleanup blocks.

Crash and power-loss tests shall terminate the process at selected points around Start, Pause, Stop, completion, migration, backup, Replace restore, and Merge restore, then verify database integrity, exactly-once recovery, and absence of silently duplicated Sessions.

## 14. Performance and scale targets

Design and verify the initial store for at least:

- 100,000 Sessions/adjustments combined.
- 1,000 active or archived organization objects.
- 1,000,000 accumulated foreground-application records.
- Ten years of daily history and common four-month/weekly/monthly Stats views.

On a representative supported SSD-based computer after warm-up:

- Start, pause, resume, stop, and ordinary settings commits: 200 ms or less excluding intentional animation.
- Home and ordinary History queries: 300 ms or less for the initial viewport.
- Four-month Stats and a seven-day timeline: one second or less.
- UI remains responsive during export, backup, restore preview, migration, and large report generation.

These are engineering acceptance targets, not promises displayed to the user. Measure them with repeatable generated datasets. Prefer indexing, projection, pagination, virtualization, and query improvement before adding denormalized aggregates.

## 15. Verification and quality gates

Every merge-quality commit must keep the solution buildable and run the most relevant checks. The stable `main` branch must eventually enforce:

- Formatting/analyzers and warnings-as-errors for repository code.
- Domain and Application unit tests, including fake-clock timing matrices.
- Real temporary SQLite integration tests, foreign-key checks, and migration tests from prior fixtures.
- Golden-file CSV/JSON/backup compatibility tests.
- Architecture dependency tests.
- View-model and formatting tests.
- Packaged Windows smoke tests for launch, single instance, notification activation, tray lifecycle, timer recovery, and clean uninstall.
- Hostile-input tests for SQL/command/markup injection, CSV formula injection, malformed and oversized JSON, ZIP traversal/decompression bombs, integer overflow, duplicate IDs, unsupported versions, and untrusted update metadata.
- Fault-injection tests for disk full, write denial, corrupted databases, interrupted atomic replacement, task/UI exceptions, forced process termination, and simulated unclean system shutdown.
- Generated-scale and bounded-memory tests that verify paging/streaming, cancellation, cache limits, and queue backpressure.
- x64 compile/package validation; platform behavior is exercised on clean Windows 11 x64 and Windows 10 22H2 x64 machines before making the corresponding support or compatibility claim.
- Accessibility review using keyboard-only operation, Windows UI Automation inspection, screen reader checks, text scaling, high contrast, and reduced motion.

Mocks should be limited to external boundaries. Prefer fakes for clocks, filesystem, update metadata, and platform event sources, and use a real temporary SQLite database for persistence behavior.

## 16. Incremental implementation order

Keep one executable path working after each increment:

1. Solution, architecture tests, Generic Host, packaged WinUI shell, CI build, settings foundation, and empty first-run routing.
2. Physical schema/migrations, organization domain, recommended-default wizard path, and basic Home channels.
3. Normal timing state machine, persistence/recovery, transport, completion workflow, tray lifecycle, and notifications.
4. Pomodoro, pause/interrupt behavior, Windows power/session adapters, and foreground-app opt-in.
5. Manual/continued/recreated Sessions, History, filtering/editing, and Recently Deleted.
6. Stats, timeline, reporting, rounding, billing, budgets, and application summaries.
7. Archive/restore/cascade deletion, export, backup/restore, palettes, full Settings, and remaining wizard paths.
8. Update adapter, portable release artifacts, accessibility completion, generated-scale performance work, and release hardening.

This order controls engineering risk only. It does not redefine product milestones or remove any non-Calendar requirement from the first release.

## 17. Remaining owner-controlled release inputs

Before publishing an intentionally unsigned portable release, the owner must approve the Unknown publisher/SmartScreen disclosure, custom license, release notes, and rollback procedure. The workflow must pass build, test, formatting, package-content, SBOM, checksum, and GitHub-attestation gates. Clean Windows 11 launch/rendering validation remains required before claiming broad compatibility. Professional legal review of the custom license is recommended but is not represented as having occurred.

No other implementation-method decision is currently blocking application scaffolding.

## 18. Primary platform references

- [Windows App SDK overview](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- [WinUI 3 getting started and supported targets](https://learn.microsoft.com/en-us/windows/apps/get-started/winui-get-started-overview)
- [.NET release and support policy](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support)
- [.NET Generic Host and dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/usage)
- [EF Core SQLite provider](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/)
- [Microsoft.Data.Sqlite transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions)
- [Windows App SDK single-instance behavior](https://learn.microsoft.com/en-us/windows/apps/develop/launch/multi-instance-apps)
- [Windows App SDK app notifications](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/)
- [Windows App SDK power management](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-power)
- [MSIX distribution choices](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/choose-distribution-path)
- [MSIX App Installer updates](https://learn.microsoft.com/en-us/windows/msix/app-installer/auto-update-and-repair--overview)
