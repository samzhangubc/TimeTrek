# TimeTrek implementation kickoff prompt

Use the following as the first request in a separate Codex project chat opened at this repository root.

---

Implement TimeTrek from the approved repository specifications. This request authorizes creating and changing application code, tests, build configuration, development packaging, and CI needed to complete the initial-release baseline. Work autonomously through coherent verified increments until the complete non-Calendar baseline is implemented; do not reinterpret incremental sequencing as permission to reduce scope.

Before changing files, read these authorities completely in this order:

1. `AGENTS.md`, if present, and `llm.txt`.
2. `docs/SDD.md`.
3. `docs/TECHNICAL_DESIGN.md`.
4. `docs/INTERFACE.md`.
5. `docs/FIRST_RUN_WIZARD.md`.
6. `docs/CONTEXT.md` and `docs/RELEASE.md`.
7. `README.md` and the current Git status/history.

Treat `docs/SDD.md` as the product-behavior authority and `docs/TECHNICAL_DESIGN.md` as the implementation-method authority. Preserve every confirmed decision. Google Calendar is deferred: create only the documented replaceable integration boundary, with no Calendar UI, OAuth dependency, or placeholder feature.

Use the approved modular-monolith stack: C# 14, .NET 10 LTS, WinUI 3 on the stable Windows App SDK, Generic Host, MVVM, SQLite through EF Core, and packaged self-contained per-user MSIX builds for Windows 10 1809+ and Windows 11 on x64 and ARM64. Maintain the dependency rules and project boundaries in the technical design. Core domain and application code must be independent of WinUI, EF Core, and Windows APIs.

Start by inspecting the available Windows/.NET toolchain and repository state. Then create a short execution plan tied to the incremental order in `docs/TECHNICAL_DESIGN.md`. Implement the first coherent vertical slice immediately; do not stop after planning or scaffolding while safe, relevant work remains. Keep an executable path running after every increment.

Engineering requirements:

- Use timestamps and the explicit timing state machine as truth; never use UI timer ticks as truth.
- Serialize timing commands and persist state transitions atomically.
- Keep views thin. UI code must not query SQLite or call Win32/WinRT directly.
- Put Windows behavior behind adapters and application ports.
- Use real temporary SQLite databases for persistence tests and fake clocks/platform sources for domain/application tests.
- Add migrations, architecture tests, analyzers, formatting, and targeted tests with the subsystem that needs them.
- Pin stable dependencies centrally and avoid unnecessary packages or abstractions.
- Preserve local-first/offline behavior, privacy defaults, raw history, accessibility, and crash recovery.
- Treat all text, files, backups, ZIP/JSON/CSV content, URIs, release metadata, and future integration data as untrusted. Use parameterized SQL and structured APIs; never interpolate input into SQL, shells, scripts, XAML/markup, paths, format strings, or unbounded regular expressions.
- Prevent spreadsheet-formula injection in exported CSV text without losing the exact source value in JSON/backups, and document the reversible CSV neutralization.
- Validate types, Unicode, lengths, ranges, JSON depth, archive paths, counts, checksums, relationships, redirects, and expanded sizes before mutation or large allocation. Defend against ZIP traversal, decompression bombs, integer overflow, malformed imports, and hostile update metadata.
- Make writes transactional and commit timing transitions before displaying success. Handle disk-full, access-denied, corruption, interrupted replace, and migration failure without reporting false success or replacing the user's store with an empty database.
- Add centralized exception observation but rely on durable state for recovery. After application crash, forced termination, Windows crash, or power loss, detect the unclean shutdown, validate the store, and recover exactly once from the last committed state. Do not attempt to continue after process-wide memory exhaustion or corrupted process state.
- Stream, page, virtualize, cancel, and bound long-running operations, collections, caches, event queues, network responses, and imports. Add backpressure/coalescing where events can outpace persistence; never accumulate unbounded foreground-app or report data in memory.
- Add hostile-input, fault-injection, forced-termination, corruption, scale, and bounded-memory tests alongside the affected subsystem. Verify deterministic cleanup of hooks, wake requests, notification icons, files, and database resources.
- Do not add a backend, accounts, telemetry, cloud sync, a Windows service, microservices, or a general plugin framework.
- Never request elevation or commit credentials, signing material, user data, databases, local paths, or build output.
- Do not publish a GitHub Release or invent a production publisher/signing identity. Development test signing is allowed; record the release-owner blocker clearly.

Repository workflow:

- This is a one-person project. Work directly on `main` unless a release is active or I explicitly request a branch.
- Preserve unrelated user changes.
- Make focused commits after coherent, verified increments and push them to `origin/main` when the worktree is clean and no release branch is active.
- Keep `README.md`, `docs/SDD.md`, `docs/TECHNICAL_DESIGN.md`, `docs/CONTEXT.md`, and `llm.txt` synchronized when implementation facts change.
- Do not create a pull request unless I explicitly ask.

Verification is part of implementation. Run the most targeted unit, integration, architecture, formatting, build, and packaging checks available after each increment. Inspect the diff for regressions, privacy leaks, dependency-direction violations, and scope creep before committing. If a platform behavior cannot be exercised in the current environment, build its adapter and contract tests, document the exact unverified check, and continue with other safe work rather than silently assuming success.

Use the complete initial-release definition of done in the SDD and technical design. The only accepted pre-release external blockers are the owner's final MSIX identity, trusted production signing method, update metadata location if needed, emergency rollback/revocation procedure, and final license/copyright review.

Begin implementation now.

---
