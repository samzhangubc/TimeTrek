# TimeTrek

TimeTrek is a planned local-first Windows desktop application that helps people understand how they spend their time. Its main workspace is inspired by a digital audio workstation (DAW) mixer: every top-level **Stream**—such as a course, job, client, research area, or hobby—occupies a simple vertical strip with its key information and time controls.

> [!IMPORTANT]
> TimeTrek is currently in the planning and documentation stage. No application implementation has been selected or started yet.

## Product goals

- Make starting, stopping, continuing, and manually adding time quick and unobtrusive.
- Show Streams side by side so their time data is easy to compare.
- Support manually adding and deleting recorded time.
- Make history browsable through filters, sorting, a calendar timeline, and weekly/monthly summaries.
- Keep an active timer available from the Windows notification area when the main window is closed.
- Store application data locally like a conventional desktop program.

## Planned experience

The home screen will use one vertical channel per Stream. Sessions can also use Stream-scoped or global Categories, Projects, valid combinations, or no organizational association.

Starting a Session will open an in-app dialog rather than another operating-system window. Exactly one Session runs at once and requires a duration. The duration field accepts minutes, hours, mixed units, and `HH:MM`. Optional Pomodoro starts at 25 minutes of work and 5 minutes of break, repeats to a chosen total duration, and remembers adjusted defaults.

History, Stats, CSV/JSON export, local backup/restore, archiving, billable time, rounding, time budgets, appearance palettes, and privacy-preserving optional features are specified in the SDD. Google Calendar is deferred, with an architectural extension boundary retained for possible later implementation.

While a timer or completion workflow is active or pending, closing the main window leaves TimeTrek available in the Windows notification area. The icon communicates elapsed Session time as far as Windows platform constraints allow. If neither timing nor completion work is active or pending, closing the window exits the application.

## Documentation

- [Software Development Document](docs/SDD.md) — current functional and technical baseline
- [Project Context](docs/CONTEXT.md) — product background, terminology, constraints, and open questions
- [First-Run Wizard](docs/FIRST_RUN_WIZARD.md) — setup screens, defaults, optional sections, and acceptance criteria
- [Interface Design](docs/INTERFACE.md) — application shell and mixer-style workspace decisions
- [Release and Auto-Update Baseline](docs/RELEASE.md) — signed GitHub Release requirements and workflow prerequisites
- [LLM Context](llm.txt) — concise repository context for coding assistants

## Repository status

The functional product requirements are finalized as one initial-release baseline rather than a reduced MVP, with Google Calendar explicitly deferred. TimeTrek targets Windows 10/11 on x64 and ARM64 from its first executable build intended for use as a per-user MSIX. Technology choices, physical data schemas, final visual values, and release-process implementation remain undecided.

Released builds will support authenticated automatic updates from signed artifacts published through GitHub Releases. The exact updater and GitHub Actions release workflow will be added after the application stack, MSIX identity, and code-signing approach are selected.

## Contributing

The intended license is PolyForm Noncommercial 1.0.0, pending addition and review of the final license/notice; until then all rights are reserved. This is a one-person project: commit focused changes directly to `main` unless a release is active, and keep the context documents synchronized whenever a product decision changes.
