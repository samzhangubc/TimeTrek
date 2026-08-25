# TimeTrek implementation prompt

Open a new Codex task at the TimeTrek repository root and paste everything below the divider as the first message.

---

You are implementing TimeTrek, a documentation-complete but code-empty local-first Windows desktop time tracker. Build the complete initial-release application described by this repository. This request authorizes in-scope local changes to source code, tests, build configuration, development packaging, and CI. It does not authorize publishing releases, pushing commits, creating pull requests, buying services, or inventing production identities or secrets.

## Goal

Deliver the complete non-Google-Calendar TimeTrek baseline as a working, tested Windows application. Work through small, coherent vertical slices, but do not treat incremental delivery as permission to reduce or defer the documented initial-release scope. Keep a runnable application path after every slice and continue while safe, relevant work remains.

## Read the repository before editing

Read these files completely before making changes:

1. `AGENTS.md`, if present, then `llm.txt`.
2. `docs/SDD.md`.
3. `docs/INTERFACE.md` and `docs/FIRST_RUN_WIZARD.md`.
4. `docs/TECHNICAL_DESIGN.md`.
5. `docs/CONTEXT.md`, `docs/RELEASE.md`, and `README.md`.
6. The current Git status and recent history.

Use this conflict order:

1. `docs/SDD.md` for user-visible behavior, scope, and invariants.
2. `docs/INTERFACE.md` and `docs/FIRST_RUN_WIZARD.md` for layout and interaction.
3. `docs/TECHNICAL_DESIGN.md` for architecture and implementation methods.
4. `docs/CONTEXT.md` and `llm.txt` for condensed rationale and guardrails.

Do not silently resolve an open decision or weaken a confirmed requirement. If two authoritative statements genuinely conflict and the order above does not resolve them, record the exact conflict and ask for the smallest owner decision needed. Exact copy, icon glyphs, and palette values may be completed with restrained, accessible choices because the specifications identify them as implementation polish.

Google Calendar is deferred. Create only the documented replaceable integration boundary. Do not add Calendar UI, OAuth packages, account connection, or placeholder controls.

## Approved implementation baseline

Use:

- C# 14 and .NET 10 LTS;
- WinUI 3 on a compatible stable Windows App SDK, never a preview SDK for release builds;
- .NET Generic Host, built-in dependency injection/configuration/logging, and MVVM;
- SQLite through the stable .NET 10-compatible EF Core provider;
- a modular monolith with the projects and dependency directions defined in `docs/TECHNICAL_DESIGN.md`;
- self-contained x64 portable GitHub Release output; Windows 11 x64 is supported without an SLA and Windows 10 22H2 x64 is compatibility-tested only; MSIX remains a future signed-installer option;
- one application instance per user.

Keep Domain and Application independent of WinUI, EF Core, filesystem/network implementations, and Windows APIs. Keep views thin: they must not query SQLite or call Win32/WinRT directly. Put persistence and Windows behavior behind the specified application ports and adapters. Pin stable dependency versions centrally and avoid unnecessary packages, interfaces, frameworks, services, or abstractions.

Do not add a backend, accounts, cloud sync, telemetry, a Windows service, microservices, a general plugin system, invoicing, productivity targets, surveillance features, elevation requests, or undocumented global action shortcuts.

## Execution

First inspect the installed Windows/.NET toolchain, repository state, and any relevant stable platform/package compatibility. Report real environment gaps without changing the approved stack. Then make a short plan using the eight-step incremental order in `docs/TECHNICAL_DESIGN.md` section 16, and begin the first executable vertical slice immediately. Do not stop after analysis, planning, or empty scaffolding.

For every slice:

1. State the user-visible capability and the affected project boundaries.
2. Implement the smallest complete end-to-end behavior, including migrations and adapters where the slice requires them.
3. Add tests with the behavior, including failure and hostile-input cases applicable to that subsystem.
4. Run the most relevant formatting, analyzer, architecture, unit, SQLite integration, build, packaging, or smoke checks available.
5. Inspect the diff for regressions, privacy leaks, dependency violations, unbounded work, and scope creep.
6. Update repository documentation only when an implementation fact or approved decision has changed.
7. Continue to the next coherent slice while the task is unblocked.

Preserve unrelated user changes. Local commits may be made only when they are focused and verified; ask before pushing, opening a pull request, publishing a package or release, or changing external repository state.

## Non-negotiable engineering rules

- Use the explicit timing state machine and durable timestamps as truth; UI refresh ticks are display-only.
- Serialize timing commands. Commit Start, Pause, Resume, Stop, automatic completion, Pomodoro transitions, and active-duration edits atomically before reporting success.
- Preserve raw timestamps/durations separately from rounded/effective values. Use signed 64-bit milliseconds for durations and integer minor units plus ISO 4217 codes for money.
- Enforce exactly one active or pending timing root with application serialization and a database constraint.
- Use `TimeProvider`/fake clocks for deterministic domain and application tests. Use real temporary SQLite databases for persistence and migration tests. Mock only external boundaries.
- Treat all strings, files, ZIP/JSON/CSV data, URIs, update metadata, and future integration payloads as untrusted. Use parameterized SQL and structured APIs; never interpolate user input into SQL, shells, scripts, XAML/markup, paths, format strings, or unbounded regular expressions.
- Bound and validate lengths, counts, numeric ranges, JSON depth, archive paths, expanded sizes, compression ratios, redirects, response sizes, collections, caches, queues, reports, imports, and exports before large allocation or mutation.
- Prevent ZIP traversal/decompression bombs and spreadsheet-formula execution. Keep CSV neutralization reversible and documented; retain exact original text in JSON and backups.
- Make multi-object operations transactional. Validate restore/import data before mutation and create the required rollback snapshot. Never replace a corrupt or partially migrated store with an empty database.
- Do not show false success after disk-full, access-denied, failed flush/replace, corruption, migration, or interrupted-write failures.
- Recover exactly once from the last committed state after application/process/Windows/PC failure. Detect unclean shutdown and validate store integrity before ordinary writes. Do not try to continue after true process-wide memory exhaustion or corrupted process state.
- Stream, page, virtualize, cancel, coalesce, or backpressure work that can grow with user data. Deterministically release hooks, wake requests, notification icons, database/file resources, and temporary files on every exit or failure path.
- Keep core tracking, history, Stats, export, backup, and restore fully usable offline. Keep optional monitoring off by default, explicit, least-privilege, local, visible, and deletable.
- Meet the documented accessibility contract: keyboard operation, Windows UI Automation metadata, screen-reader labels, system text scaling, non-color cues, and built-in/default presentation targeting WCAG 2.2 AA principles.

## Required verification and completion bar

Implement and verify every non-Calendar requirement in the SDD, including organization and mixer Home; normal and Pomodoro timing; completion and Windows notification-area lifecycle; manual, continued, recreated, and negative-adjustment records; History and Recently Deleted; Stats and Timeline; billing, rounding, budgets, archiving, cascade deletion/restore; CSV/JSON export; backup/restore; appearance; first-run wizard; Settings; optional foreground-app tracking; updates; accessibility; recovery; and self-contained x64 packaging.

Use the exact quality gates, scale targets, hostile-input tests, fault-injection tests, forced-termination checks, platform adapter tests, and definition of done in `docs/SDD.md` and `docs/TECHNICAL_DESIGN.md`. If the current environment cannot exercise a Windows/package behavior, implement its boundary and contract tests, record the exact unverified command or manual check, and continue with other safe work. Never claim a check passed when it did not run.

For version 1.0.1, build an intentionally unsigned self-contained portable x64 archive through the tag-bound GitHub workflow. Publish SHA-256 checksums, SPDX SBOM, GitHub provenance/SBOM attestations, legal notices, and a conspicuous Unknown publisher/SmartScreen disclosure. Do not publish an unsigned MSIX or claim a verified Windows publisher. A future signed installer requires separate approval and signing inputs.

In progress updates, report completed behavior, validation results, current risks, and the next slice. In the final handoff, list completed capabilities, changed files/projects, commands and outcomes, remaining owner blockers, and any platform checks that still require supported physical or hosted Windows machines.

Begin implementation now.

---
