# Thyme-Me Project Context

## Why this file exists

This is the durable product context for contributors and future development sessions. It separates confirmed direction from implementation proposals and unresolved decisions. Update it with `docs/product/SDD.md` and `docs/engineering/llm.txt`; never turn an assumption into a requirement silently.

## Current product statement

Thyme-Me is a local-first Windows desktop application for recording and understanding how time is spent. Students are the initial audience, but the organization model also supports work, research, freelance, and personal activities.

The home screen uses a DAW-mixer metaphor: each top-level **Stream** appears as a simple vertical strip with its main time data and controls.

## Confirmed terminology and organization

- **Stream:** The top-level tracked context and mixer column. Examples: a course, job, client, research area, or hobby.
- **Scoped Category:** A reusable activity type belonging to exactly one Stream. Course examples: lecture, tutorial, homework, and exam study.
- **Global Category:** An aspect of life with no Stream parent. Examples: sleep, exercise, and commuting.
- **Project:** A longer-lived body of work such as a course project, dataset, or research paper. It may stand alone or optionally belong to one Stream.
- **Session:** One recorded block of time.
- **Stray Session:** A Session with no Stream, Category, or Project association.
- **Completion description:** Optional, potentially multiline text describing what was completed.
- **Billable Session:** A Session marked chargeable, with or without a wage.

A Session can be stray; use only a Stream, Project, or global Category; or use a valid combination. A scoped Category requires its parent Stream. A global Category does not become owned by a Stream merely because the same Session references both.

## Confirmed tracking workflow

- Starting a Session opens a dialog inside the application, not a separate top-level window.
- A bare duration means minutes; an `h` suffix means hours.
- A bounded Session stops automatically at its duration.
- Every Session has timestamps/date information sufficient for history and timeline placement.
- A previous Session can be continued as a new independent Session.
- Common manual entries can be recreated quickly with a new date/time.
- Finishing tracked time or adding time manually opens an in-app completion view.
- `Enter` saves and exits the completion view, including when the description is empty.
- `Shift+Enter` inserts a line break for multi-paragraph descriptions.
- Pomodoro is available beside the normal timing control in the start dialog.
- Pomodoro defaults to 25 minutes of work and 5 minutes of break, adjustable before start.

## History, timeline, and Stats

- Session history is built in, not an optional add-on.
- History filters include Stream, Category, Project, duration range, and date/date range; billability and origin are available when applicable.
- History sorting includes date, duration, and relevant displayed names in either direction.
- Every history entry shows its date and duration.
- The Stats page contains a day/hour timeline using locally stored Sessions.
- The Stats page includes weekly/monthly summaries and graphs such as proportional breakdowns by Stream, Project, Category, or billability.
- Productivity targets are explicitly excluded.

Stats defaults to a four-month Daily heatmap and provides Week, Month, Custom, Weekly, Cumulative, and Timeline views. Raw actual time is the default reporting basis, with an explicit Effective/Rounded basis available. Timeline geometry always follows raw timestamps, and editing occurs through History.

## Billing, time budgets, and rounding

- Sessions can be billable or non-billable.
- Hourly wage is optional.
- Historical Sessions preserve the rate/status applied at creation rather than changing with later defaults.
- Optional **time budgets** belong to Streams and Projects, are non-recurring by default, and may reset weekly or monthly. They do not block tracking or generate alerts.
- Time rounding is configurable in Settings and per Session.
- Rounding is off by default and supports increments such as nearest 5 or 10 minutes.
- Raw timestamps/duration remain preserved when a rounded duration is used.

Rounding offers 5/10/15/30-minute presets and custom 1–60-minute increments, uses banker's rounding at midpoints, and is configured in the first-run wizard and Settings. Billing uses rounded time. Stats defaults to raw actual time with an explicit Effective/Rounded basis; History and exports expose both values.

## Windows lifecycle and interruption behavior

- Closing the main window during an active Session hides it to the Windows notification area and tracking continues.
- Closing when nothing is active or pending exits the application.
- `Ctrl+W` invokes that same state-dependent close behavior.
- Only documented keyboard behavior is allowed: contextual navigation, search, timeline zoom, accessibility, layout, close, and quit commands. Hidden global action shortcuts are excluded.
- Optional idle/display interruption handling is configured in Settings.
- When enabled and Thyme-Me is foreground during a Session, it requests that Windows prevent automatic sleep and monitor power-off.
- Explicit user sleep, lock, sign-out, shutdown, or display-off actions are never blocked.
- If display-off, lock/screen-saver, or suspension is detected, the enabled behavior stops the Session at the transition and notifies the user on next activity/resume.

Windows' documented wake-prevention API does not suppress screen savers, so the selected framework must implement and test screen-saver/session-lock handling separately.

## Optional foreground-application tracking

- Off by default.
- Enabled only through an explicit action and full disclosure in Settings or the optional Advanced wizard.
- Collects only during an active Session.
- Baseline data is application identity plus foreground duration.
- No keystrokes, screenshots, clipboard data, content, or browser history.
- No permission request occurs until the user enables the feature, and then only the minimum non-administrator access is requested. The initial feature never elevates.
- Stored locally, visibly indicated while active, and deletable by the user.

Only application display name and executable filename are stored. Window titles, full paths, and browser domains are excluded. A per-Session application breakdown appears at completion. Thyme-Me never elevates for this feature; inaccessible elevated applications accumulate under **Unknown elevated application**.

## Calendar integration

The built-in timeline is local and always available. Optional Google Calendar integration is feasible without a Thyme-Me backend using Google's desktop OAuth flow.

- The connection is off until the user explicitly authorizes it.
- Authorization occurs through the system browser.
- Tokens are protected locally and removable through Disconnect.
- Thyme-Me requests the narrowest scope; read-only is the maximum initial scope until write behavior is approved.
- Calendar failure or offline operation never breaks local tracking or reporting.
- Google may require OAuth consent configuration and public-app verification.

Google Calendar is deferred from the initial implementation. The architecture retains a replaceable integration boundary for a possible later backend-free, read-only overlay with event-to-manual-Session conversion, user-selected calendars, and labelled offline cache.

## Data ownership and portability

- Primary data and settings are stored locally in an appropriate per-user Windows application-data location.
- CSV export is required.
- JSON export follows the finalized CSV field semantics while using appropriate JSON structures/types.
- CSV has a versioned rich baseline with raw and rounded times, organization, description, billing, timing, and timezone fields; UTF-8/RFC 4180/ISO 8601 conventions; and All or Current Filtered export. JSON is a versioned structured counterpart.
- CSV and JSON include a concise Thyme-Me data dictionary so humans and LLMs can interpret the values.
- Local backup and restore use user-selected files and work offline.
- Restore validates before changing live data and makes merge/replace behavior explicit.
- Exports and backups do not silently include credentials or OAuth tokens.

## Archiving

- Streams, Categories, and Projects can be archived and restored.
- Archived objects disappear from active selectors/mixer views by default.
- Their historical Sessions remain intact and filterable.
- Archiving never rewrites historical names, relationships, billing, or totals.

## Appearance

- Appearance modes: Light, Dark, Follow Windows, and scheduled light/dark switching by local time.
- GitHub-inspired Light/Dark is the default palette family.
- Each light or dark palette variant has three primary user-facing colors: Canvas, Surface, and Accent.
- Supporting text, border, state, chart, and semantic colors are derived/system-defined with accessibility checks.
- Common presets appear first in the selector.
- Initial order: GitHub Default, Codex Plus, Catppuccin, Gruvbox Medium, Solarized, Claude-inspired Warm, Google-inspired Material, Nord-inspired, Dracula-inspired, Monokai-inspired.
- Custom palettes accept plain `#RRGGBB` values in Settings; saved/imported presets use versioned JSON.
- Contrast is adjustable with live preview and accessibility warnings.

Official OpenAI documentation did not establish a Codex palette inventory. The Codex-derived set was verified from the locally installed Codex package `26.818.2872.0`: GitHub Light/Dark Default, Light+/Dark+, Catppuccin Latte/Mocha, Gruvbox Light/Dark Medium, and Solarized Light/Dark. A later visual-design pass will finalize exact Thyme-Me values.

## Confirmed decision baseline — 2026-08-20

- Self-contained x64 portable GitHub Release; one instance per user. Windows 11 x64 is supported without an SLA. Windows 10 22H2 x64 is a technical-compatibility target only, with no promise of Windows 10-specific fixes.
- One active Session globally. A duration is required; supported formats include bare minutes, hours, mixed units, minute suffixes, and `HH:MM`. Warn at 24 hours but continue. Pre-start Cancel creates no Session; after start, Stop saves elapsed time.
- A Session supports at most one Stream, multiple Categories, and one Project. Category scope changes prompt for the desired valid historical behavior.
- Stream strips show active timing, start/stop, manual add, negative/delete time, next-Session Category/Project defaults, and today/week/total. Negative/delete time asks between selecting exact Sessions and making a negative adjustment.
- Archiving a Stream automatically archives its scoped Categories and owned Projects. Projects cannot have multiple owning Streams, but standalone Projects can be used with several Streams; those shared Projects appear as optional, unchecked archive choices with a cross-Stream impact warning.
- Continue copies associations, billability, and wage. Quick manual recreation copies associations only.
- Hidden completion uses a Windows notification that restores the in-app completion view; pending descriptions queue; descriptions remain editable.
- `Ctrl+Q` explicitly quits with active-Session confirmation. The window close control, `Alt+F4`, and `Ctrl+W` share state-dependent hide/exit behavior. The notification-area timer uses compact `h:mm` plus an exact tooltip.
- Pomodoro repeats to a requested total cycle duration, asks total/work/break values, remembers the most recent work/break defaults, uses a five-minute work buffer before break confirmation, requires confirmation for each new work interval, and asks whether separately stored breaks are billable. Manual pauses freeze and do not consume the total countdown.
- Idle/display handling and configurable generic inactivity default on, with the user's detailed choices finalized in first-run setup. Resume uses a notification into pending completion.
- History uses OR within dimensions and AND between dimensions, searches descriptions and organization names, defaults newest-first, supports full editing, and provides confirmation plus Undo for deletion.
- Categories provide the default billability for Sessions; a Session may override it at start. Multiple currencies are supported. Earnings are estimates only; invoicing is excluded.
- Conflicting billing defaults from multiple selected Categories must be resolved explicitly before start; selection order never wins silently.
- Category scope changes and Project moves prompt between previewed historical migration or archiving the original and creating a replacement.
- Permanent organization deletion handles one root at a time, previews counts and expandable cascade details, and requires the exact root name for a large cascade. The complete unit enters the shared configurable-retention Recently Deleted area and restores atomically; archiving remains the ordinary safe path.
- Current organization names appear in History while Sessions retain stable IDs and optional original-name snapshots for export and audit.
- Explicit quit stops and saves after confirmation; crash recovery reconstructs active bounded Sessions and uses the scheduled end when it has passed.
- Negative corrections are signed adjustment records and may make totals visibly negative. Manual Sessions partly or wholly in the future warn but are allowed after confirmation.
- Pomodoro shortens its final work interval, caps boundary overtime at five minutes before automatic break recording, supports defined Pause/Skip behavior, and preserves partial segments when stopped.
- Multi-Category additive chart time appears once as Multiple Categories and drills into combinations/Sessions; a separate Category-involvement view is explicitly non-additive. Stray time is Uncategorized; earnings remain grouped by currency; cross-midnight rounded time is allocated proportionally.
- Time budgets count raw actual time toward both associated Stream and Project, exclude breaks, include negative adjustments, and reset on local calendar boundaries. Billing uses rounded effective time.
- CSV keeps one Session row with JSON-array Category cells; an explicit per-export **Include application breakdown** option, off by default, adds the companion application CSV, while JSON nests the same opted-in records. Backups are versioned ZIP containers with checksums and deterministic merge remapping.
- Backups include all non-secret app data and settings, including opted-in application records after disclosure, are not application-encrypted, and restore by previewed Replace or Merge (Replace default).
- Appearance warns but permits low-contrast custom palettes after explicit acknowledgement. Built-in/default palettes target WCAG 2.2 AA; acknowledged custom palettes are a contrast exception. Scheduled mode defaults light at 07:00 and dark at 19:00.
- The first-run wizard covers all user defaults, including Sunday/Monday week start, rounding, idle handling, and start with Windows. All choices remain editable.
- The finalized wizard flow is recorded in `docs/product/FIRST_RUN_WIZARD.md`: **Use recommended defaults** fast path, required step-by-step basic setup, genuinely optional Advanced setup, restart-before-basic-completion with saved values prefilled, rerunnable settings, optional MATH 100 and Work samples, overwrite-on-type 25-minute default, last-used timing mode, Windows-derived regional/accessibility choices, optional billing and activity tracking, and hover/focus tooltips after setup.
- Version 1.0.2 uses manual updates from an intentionally unsigned, self-contained x64 portable archive published through GitHub Releases. Releases built from current source expose only the ZIP; its extracted root contains the `thymeme.exe` launcher and `app`, `legal`, and `metadata` folders. SHA-256 is published in release notes, GitHub attestations target the ZIP, and provenance, notices, disclosure, and SPDX SBOM are bundled below the root. Windows may show Unknown publisher or SmartScreen.
- `docs/release/RELEASE.md` records the verified repository state, release workflow, integrity checks, unsigned-package disclosure, and safe future-update boundary.
- First-party licensing is the custom Thyme-Me Source-Available License 1.0 (source-available, not open source). Official binaries are free of charge for permitted personal, educational, and internal noncommercial use; redistribution, product incorporation, commercial use, and derivative products require prior written permission.
- Product decisions precede a separate technology-stack comparison.
- Interface design is active. `docs/product/INTERFACE.md` defines the non-collapsible icon-only navigation column (64 px default), dedicated Archive destination, remembered 1280×800 initial shell with 960×640 minimum, approximately six visible Studio One-inspired Stream channels, and a persistent bottom Session transport bar.
- Practical region and column widths are draggable, saved immediately, and restored automatically. Stream channels share one width, so dragging any channel edge resizes every channel. `Ctrl++`, `Ctrl+-`, `Ctrl+0`, `Ctrl+wheel`, and pinch scale the whole interface from 80–150%, persist the choice, and re-layout content within the viewport; Settings can reset layout defaults.
- Currency input uses a searchable named-currency list with CAD and USD pinned first and all other currencies alphabetized; the persistence model remains ISO code plus banker's-rounded minor units.
- History is a virtualized continuous table with sticky date groups, one dense header row, removable filter chips, persistent checkboxes, and an optional draggable right inspector. It defaults to All time/newest first. Column order is Time, Duration, Stream, Category, Project, Description, Origin, actions; widths save but columns do not reorder, and lower-priority columns hide at narrow widths.
- History uses one-line descriptions, first Category plus `+N`, effective duration in-table with raw/rounding details in the inspector, signed warning-styled adjustments, conditional billing fields, an optional earnings column, and origin icons. Normal Delete confirms; `Shift+Delete` skips confirmation but still moves the Session into the dedicated recoverable Recently Deleted view.
- Stats follows the owner-provided activity-dashboard hierarchy with slightly larger spacing: five exact-time headline metrics, a fixed-height four-month daily heatmap with Daily/Weekly/Cumulative/Timeline views, then equal fixed-width Time insights and ranking/breakdown columns that stack narrowly. Selecting a heatmap day opens a right inspector with Sessions and a Stream pie. Rankings show both Top Streams and Top Projects; conditional billing, per-currency earnings, and budget progress remain below the headline strip.
- Stats Timeline is a read-only seven-day vertical time grid within the fixed activity region. It auto-fits active hours with a 24-hour toggle, supports hour-line zoom through buttons/Ctrl+wheel/touchpad pinch, horizontally scrolls days narrowly, shows fixed day totals and current time, and uses low-alpha Stream blocks with origin icons. Overlaps are side by side; cross-midnight and paused Sessions split visibly. Stored Pomodoro breaks and the labeled work-time buffer use light shades. Empty-space selection starts manual entry, filters dim, and editing opens History.
- Archive defaults to Streams within All/Streams/Categories/Projects tabs and uses persistent search, filters, sort, checkboxes, expandable Stream children, and a saved-width right inspector. Rows expose restore plus filtered History/Timeline navigation; global Categories have their own section and owned items show their Stream.
- Stream restore selects children archived with the parent but leaves independently archived children unselected; restoring an archived child includes its owner when required. Compatible bulk restore is supported. Active conflicts can queue Archive after Session stops, with a visible pending state and Cancel.
- A Project is standalone or owned by exactly one Stream. When a Stream is archived, standalone Projects used across Streams are listed separately, remain unchecked by default, and warn that archiving affects active use everywhere.
- Archive permanent deletion is one root at a time through a cascade preview and danger confirmation; large cascades require the root name. Organization and Session deletions share one configurable Recently Deleted retention period, and an organization cascade restores atomically.
- Settings uses a responsive card dashboard with General first, no internal section sidebar, persistent global search, immediate ordinary saves with quiet status, Advanced disclosures, no bottom transport, and one-column narrow reflow. Cards follow General, Appearance, Timing, Pomodoro, Billing, Activity & Privacy, Windows, Data, Updates, and About.
- Recently Deleted defaults to 30 days, supports 7/14/30/60/90-day presets and custom 1–365 days, shows purge deadlines, and purges automatically. A large organization cascade begins at five affected organization objects or 25 Sessions.
- Settings provides live appearance/rounding previews, progressive billing controls, privacy/storage disclosure, update status, data tools, preference-only reset, sample-data creation, safe wizard rerun, and content-excluding diagnostics. Google Calendar has no placeholder UI before it ships.
- Built-in/default presentation targets WCAG 2.2 AA principles where applicable, complete keyboard access, Windows UI Automation, screen-reader labeling, system text scaling, and non-color state cues. Explicitly acknowledged custom palettes may violate contrast only.
- All non-Calendar functional requirements form one initial-release baseline rather than a reduced MVP. Implementation may be sequenced incrementally. Windows 11 x64 is the supported release platform; Windows 10 22H2 x64 remains compatibility-only.

## Technical guardrails

- Timestamps, not UI update ticks, are the source of truth.
- Preserve raw time separately from rounded/effective time.
- Keep Session descriptions and foreground-app records attached to the exact originating Session.
- Treat every optional integration/monitoring feature as explicit opt-in and least-privilege.
- Core features must remain usable offline.
- Archive rather than delete when the user's intent is to remove completed organization objects from active use.
- Never store secrets in exports, backups, the repository, or installed-program directories.
- The approved implementation is a C# 14/.NET 10 LTS modular monolith using WinUI 3, stable Windows App SDK, Generic Host/MVVM, SQLite through EF Core, and self-contained x64 portable artifacts. MSIX project tooling remains available for development and a future signed installer. `docs/engineering/TECHNICAL_DESIGN.md` is the implementation-method authority.
- Treat all input and imported data as untrusted. Prevent SQL/command/markup/path/regex and spreadsheet-formula injection, hostile ZIP/JSON allocation, overflow, and unbounded resource growth.
- Durable committed state is the recovery authority after application failure, forced termination, Windows crash, or power loss. Never silently replace a corrupt store or show successful persistence that did not complete.

## Decisions still requiring owner input

- Remaining focused-dialog visual polish and exact copy.
- Clean Windows 11 portable-launch/rendering evidence and optional Windows 10 compatibility evidence.
- Emergency rollback/revocation procedure and final release protection.
- Final license/copyright notice review before distribution.

## Repository conventions

- This is a one-person project. Commit directly to `main` unless a release is active or the owner explicitly requests a branch.
- Implementation is authorized using `docs/engineering/TECHNICAL_DESIGN.md`; `docs/engineering/IMPLEMENTATION_PROMPT.md` is the prepared kickoff for a separate project chat.
- Update `README.md`, `docs/SDD.md`, this file, and `llm.txt` when a decision makes them inaccurate.
- Label proposals and unresolved decisions explicitly.
- Never commit secrets, local databases, build output, or machine-specific configuration.

## Context history

### 2026-08-24 — Responsive usability implementation

- Implemented a viewport-aware first-run wizard and responsive Home, transport, History, Stats, Archive, and Settings layouts with a 960×640 minimum window and remembered bounds.
- Added whole-interface scaling, safe state-dependent transport actions, Enter-to-complete behavior, per-Stream association defaults, configurable Stream budgets, named currency selection, Stats range/view controls, and a live timer tray icon.
- Expanded `docs/engineering/CODE_MAP.md` and `docs/engineering/feature-index.json` with direct ownership routes for these capabilities.

### 2026-08-23 — First-principles consistency audit

- Resolved Pomodoro pause timing, setup completion, documented shortcut scope, close behavior, raw-default Stats and budgets, multi-Category drill-down, non-elevating activity tracking, explicit activity-export inclusion, update-control semantics, and low-contrast accessibility exceptions.
- Confirmed one unified non-Calendar initial-release baseline, self-contained x64 packaging, Windows 11 support without an SLA, and best-effort Windows 10 22H2 compatibility.

### 2026-08-20 — Initial baseline

- Connected the workspace to `samzhangubc/Thyme-Me`.
- Recorded the Windows desktop, mixer, timer, notification-area, and local-storage direction.

### 2026-08-20 — Organization, notes, and billing

- Added Streams, scoped/global Categories, Projects, stray Sessions, completion descriptions, billability, and hourly wage.

### 2026-08-20 — History, reporting, optional integrations, and appearance

- Added Continue, history filters/sorts, Session dates, timeline/Stats, Pomodoro, idle/display handling, archiving, export, backup/restore, rounding, foreground-app tracking, time budgets, and palettes.
- Confirmed backend-free Google Calendar feasibility; its read-only overlay and event-conversion behavior were finalized in the later owner questionnaire.
- Explicitly excluded targets and additional keyboard shortcuts.

### 2026-08-20 — Owner questionnaire decisions

- Incorporated all 75 responses into the SDD and context.
- Scoped the next product-design pass to the first-run setup wizard.

### 2026-08-20 — First-run wizard baseline

- Finalized the setup structure and defaults; Google Calendar is deferred and absent from the initial wizard.
- Originally specified signed automatic updates; superseded for 1.0.1 by manual, attested unsigned portable GitHub Releases on 2026-08-25.

### 2026-08-20 — Deferred Calendar and billing conflicts

- Deferred Google Calendar from the initial implementation while retaining an integration boundary for later work.
- Required explicit Session-start resolution when multiple Categories supply conflicting billing defaults.

### 2026-08-20 — Functional closure audit

- Resolved organization migration/deletion, lifecycle recovery, negative adjustments, Pomodoro edge cases, reporting allocation, budgets, exports, backup merge, foreground tracking, pending completion, and update-channel behavior.
- Finalized Pomodoro total-duration semantics, break privacy, historical budget counting, earnings rounding, future manual entries, restore/delete recovery, and packaged CSV documentation.
- Closed the functional product baseline and moved the active product phase to interface design.

### 2026-08-20 — Interface shell direction

- Chose a fixed, non-collapsible, 64 px icon-only primary navigation column with tooltips.
- Made Archive a primary destination and set remembered window bounds with a 1280×800 first-launch size and 960×640 minimum.
- Adopted a Studio One console-inspired horizontal channel structure with substantially wider, text-readable Thyme-Me strips.
- Moved the authoritative active timer and Start/Pause/Stop controls to a persistent bottom transport bar. Stream channels show only their own time and retain direct Start Timer actions.
- Finalized the transport as a 88–96 px persistent row (two rows at minimum width), with timer left, Session selectors/configuration centered, and controls right; it is present everywhere except Settings.
- Duration is prompted at every start with overwrite-on-type `25`, then remains editable during a normal active Session to move its automatic stop boundary.
- Finalized channel totals/state, persistent progress-rail alignment, Total/Today/This Week summary order, conditional vertical budget bars, labelled adjustments, horizontal scrolling, reordering, Add Stream surfaces, and right-side editing inspector.
