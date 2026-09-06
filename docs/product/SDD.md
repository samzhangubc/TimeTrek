# Thyme-Me Software Development Document

| Field | Value |
| --- | --- |
| Document status | Working product specification; unresolved decisions are marked explicitly |
| Product status | Functional, interface, and technical baselines approved; modular implementation active |
| Target platform | Windows desktop |
| Last updated | 2026-08-24 |

## 1. Purpose

This document is the implementation baseline for Thyme-Me. It records confirmed product behavior, safety constraints, data concepts, exclusions, and decisions that still require the product owner's input. An implementation must not silently resolve an open decision.

Future product decisions must update this document, `docs/product/CONTEXT.md`, and `docs/engineering/llm.txt` together.

## 2. Product summary

Thyme-Me is a local-first Windows time tracker intended primarily for students while remaining useful for employment, research, freelance work, and personal life. It organizes time around **Streams**, which appear side by side as vertical channel strips inspired by a DAW mixer.

A Stream can represent a course, job, client, research area, hobby, or other ongoing context. Sessions may also be associated with Categories and Projects or recorded without any association. The primary experience must remain low-friction even as optional organization, reporting, billing, calendar, and activity-tracking features are added.

## 3. Product principles

- **Fast capture:** starting, finishing, continuing, or manually adding time should take very few actions.
- **At-a-glance comparison:** the mixer layout should make Streams easy to compare.
- **Local ownership:** the primary data store, exports, and backups belong to the user and live on their device.
- **Progressive complexity:** billing, Pomodoro, Google Calendar, time budgets, and application-focus tracking must remain optional.
- **Historical truth:** edits, rounding, rate changes, and archive operations must not silently corrupt original timestamps or reinterpret old Sessions.
- **Explicit privacy:** no account connection, foreground-application monitoring, or permission request may occur without a user action that enables the relevant feature.

## 4. Scope

### 4.1 In scope

- Windows desktop application with a mixer-style Stream home screen.
- Tracked and manually entered Sessions.
- Required-duration timers and adjustable Pomodoro timing.
- Session-completion descriptions.
- Streams, scoped or global Categories, Projects, and stray Sessions.
- Billable status and optional hourly wage.
- Searchable, filterable, sortable Session history.
- Calendar-style timeline and statistical summaries.
- Deferred extension boundary for a possible later backend-free Google Calendar integration; no Calendar feature ships initially.
- Optional idle/display-state handling and application-focus tracking.
- Archiving for Streams, Categories, and Projects.
- Configurable time rounding, disabled by default.
- CSV and structurally corresponding JSON export.
- Local file backup and restore.
- Optional time budgets.
- Preset and custom three-color appearance palettes with light/dark behavior.
- Windows notification-area operation while a timer or completion workflow is active.

### 4.2 Explicitly excluded at this stage

- User accounts, Thyme-Me-hosted backend services, and Thyme-Me cloud synchronization.
- Mobile, web, macOS, or Linux clients.
- Multi-user collaboration, managers, permissions, timesheet approvals, payroll, and workforce surveillance.
- Productivity targets, streak targets, or required time goals.
- GPS, screenshots, keystroke capture, and content inspection.
- Undocumented global action shortcuts. Documented contextual navigation, search, timeline zoom, accessibility, layout, close, and quit shortcuts are permitted.
- Invoicing, expense tracking, employee scheduling, time off, or accounting integrations.

Google Calendar is the only currently approved optional external integration. It must not become a dependency for local tracking or the built-in timeline.

## 5. Users and primary use cases

### 5.1 Primary user

A student who wants a low-friction record of study time. The same model should also support a worker, researcher, freelancer, or individual tracking personal activities.

### 5.2 Core use cases

1. View Streams and their primary data side by side.
2. Start or finish a Session for a Stream or another valid association.
3. Start a default or adjusted Pomodoro Session.
4. Continue a previous Session as a new Session.
5. Manually add or quickly recreate a common time entry.
6. Optionally describe what was completed when time is saved.
7. Review, filter, sort, edit, or delete Session history.
8. Review time on a daily timeline and in weekly or monthly summaries.
9. Mark time billable and optionally associate a wage.
10. Export data or create and restore a local backup.
11. Archive completed Streams, Categories, or Projects without losing history.
12. Optionally compare tracked time with time budgets.
13. Optionally record which foreground applications were used during a Session.

A possible later Google Calendar overlay is an extension use case rather than an initial product use case.

## 6. Functional requirements

Requirements use stable identifiers so later decisions, implementation work, and tests can reference them.

### 6.1 Streams and mixer home screen

- **FR-001:** The system shall represent each top-level tracked context as a **Stream**.
- **FR-002:** The home screen shall display Streams as separate vertical strips in a horizontally arranged, mixer-inspired layout.
- **FR-003:** Each Stream strip shall display the Stream's primary time data.
- **FR-004:** Each Stream strip shall provide actions to start timing, finish timing, add time, and delete time.
- **FR-005:** A Stream shall be able to include zero or more scoped Categories.
- **FR-006:** A Project may stand alone or optionally belong to exactly one Stream.

Each Stream strip shall show the active timer; start/stop, add-Session, and negative-time/Session controls; Category and Project defaults for the next Session; and today, current-week, and all-time totals. Stream creation/editing uses the right inspector defined in `docs/product/INTERFACE.md`; Home ordering, shared draggable channel width, horizontal overflow, and empty states are also finalized there.

### 6.2 Organization hierarchy and Session associations

- A **Stream** is the entity represented by one mixer column. Examples include a course, job, client, research area, or hobby.
- A **scoped Category** belongs to exactly one Stream. Examples within a course include lecture, tutorial, homework, and exam study.
- A **global Category** has no Stream parent. Examples include sleep, exercise, commuting, or another aspect of life.
- A **Project** is a distinct body of work tracked over time. Examples include a course project, dataset, or research paper.
- A **stray Session** has no Stream, Category, or Project association.

- **FR-007:** A Category shall support exactly one of two scopes: scoped to one Stream or global with no Stream parent.
- **FR-008:** Each tracked or manually added Session shall support no association; an association with only a Stream, Category, or Project; or a valid combination of Stream, Project, and Category.
- **FR-009:** A Session using a scoped Category shall also be associated with, or unambiguously inherit, that Category's parent Stream.
- **FR-010:** A global Category shall not require a Stream and shall remain global when used on the same Session as a Stream.

| Session association | Valid? | Constraint |
| --- | --- | --- |
| None | Yes | Stored as a stray Session |
| Stream only | Yes | None |
| Global Category only | Yes | No Stream required |
| Project only | Yes | Project must be independently selectable |
| Stream + Project | Yes | Project is standalone or belongs to that Stream |
| Stream + scoped Category | Yes | Category must belong to that Stream |
| Stream + scoped Category + Project | Yes | Category must belong to that Stream |
| Global Category + Project | Yes | No Stream required |
| Stream + global Category | Yes | Category remains global |
| Stream + global Category + Project | Yes | Category remains global |
| Scoped Category without its parent Stream | No | Parent must be selected or inferred |
| Scoped Category with the wrong Stream | No | Parent mismatch must be rejected |

A Session may reference at most one Stream, multiple Categories, and at most one Project. All selected scoped Categories and an owned Project must be compatible with the selected Stream. A Project may be standalone or owned by exactly one Stream; multiple owning Streams are not supported. A standalone Project may nevertheless be used by Sessions associated with several different Streams.

Changing a Category's scope after it has Sessions opens a preview and asks whether to archive the original and create a replacement or migrate all historical Sessions to the new scope. Moving a Project to another Stream after it has Sessions uses the same preview and replacement-or-migration choices. Neither action may rewrite history silently.

Streams, Categories, and Projects may be permanently deleted even when they have Sessions. Permanent organization deletion is limited to one root object at a time; bulk permanent deletion is not supported. The application must preview affected object and Session counts, provide expandable cascade details, and require confirmation. A cascade is **large** when it affects at least five organization objects or at least 25 Sessions; a large cascade additionally requires the root object's exact name. The complete cascade moves as one unit to the shared **Recently Deleted** area used by Sessions and organization data. One configurable retention period governs every Recently Deleted item. Restoring an organization deletion restores the complete cascade atomically; after retention purge, deletion is irreversible. Archiving remains the ordinary non-destructive removal path.

Renaming an organization object changes the name shown throughout current UI and History. Sessions retain stable organization IDs and an optional original-name snapshot so exports and audits can identify the name used when the Session was recorded.

### 6.3 Session timing

- **FR-020:** Starting a timer shall present a dialog within the main application surface, not a separate operating-system window.
- **FR-021:** A bare duration value in the start dialog shall mean minutes.
- **FR-022:** A duration ending in `h` shall mean hours.
- **FR-023:** Ambiguous, invalid, or non-positive bounded durations shall not start a Session.
- **FR-024:** A bounded Session shall finish automatically when its requested duration is reached.
- **FR-025:** The user shall be able to finish an active Session manually.
- **FR-026:** Displayed elapsed time shall derive from timestamps rather than accumulated UI ticks.
- **FR-027:** Every Session shall store start and end timestamps and sufficient local-time-zone context to reproduce its calendar date and timeline position.
- **FR-028:** A manually entered Session shall store a date in addition to its entered duration or start/end values.
- **FR-029:** Continuing a previous Session shall create a new independent Session and shall not reopen or extend the historical Session.

Exactly one Session may be active globally. A positive duration is required for every started Session; open-ended stopwatch Sessions are not supported. Accepted formats are bare minutes, `h`, decimal hours, mixed hours/minutes, minute suffixes, and `HH:MM` (for example `45`, `1.5h`, `1h 30m`, `90m`, and `01:30`). Input trims surrounding whitespace, accepts both `.` and the Windows regional decimal separator, and interprets `HH:MM` as hours and minutes. Invalid input shows accepted-format examples. The application warns at 24 hours but allows the Session to continue. Pause/resume is supported within one Session, with paused time excluded. **Cancel** is available only before a Session starts and dismisses the start workflow without creating a record. Once timing begins, **Stop** is the terminating action and always saves elapsed time. Manual overlaps and any manual Session extending partly or wholly into the future warn but are allowed after confirmation. Sessions crossing midnight remain one Session while Stats and the timeline split their duration by local day.

Explicit quit during an active Session offers **Stop, save, and quit** or **Cancel**. After a crash or forced shutdown, Thyme-Me reconstructs the active Session from persisted timestamps. If its requested end has passed, it ends at the scheduled end and queues the normal completion workflow; otherwise it resumes as active.

#### Continue previous Session

- **FR-030:** The history and relevant recent-entry surfaces shall provide a Continue action.
- **FR-031:** Continue shall prefill the new Session from the selected historical Session's organizational and billing associations.
- **FR-032:** Continue shall not copy the historical Session's timestamps or completion description.

Continue copies associations, billability, and wage only; it does not copy description, requested duration, Pomodoro settings, timestamps, or rounding. Quick manual recreation copies associations only and requires all other values anew.

### 6.4 Pomodoro mode

- **FR-040:** The start-Session dialog shall expose Pomodoro mode beside the normal timing-mode/drop-down control.
- **FR-041:** Pomodoro shall default to a 25-minute work interval followed by a 5-minute break.
- **FR-042:** The user shall be able to adjust work and break durations before starting Pomodoro mode.
- **FR-043:** Work and break intervals shall be distinguishable in stored data and in the active UI.
- **FR-044:** Pomodoro shall remain optional and shall not change the default non-Pomodoro Session workflow.

Pomodoro repeats work/break cycles until a required total cycle duration is reached. The total is a hard cap over work, breaks, and five-minute boundary buffers; time spent manually paused is excluded because pausing freezes the active interval and the total countdown. The start prompt asks for total duration plus work and break durations. Work/break defaults begin at 25/5 minutes, and the most recently entered work/break values become future defaults. If the remaining cycle time cannot contain a full interval, the current/final segment is shortened to the remaining time; reaching the cap saves that partial segment and starts no further interval.

At a work interval boundary, Thyme-Me continues recording work for a maximum five-minute buffer while waiting for the user to return and confirm the break. If the user does not return, work stops at boundary plus five minutes and break recording begins automatically. Each next work interval still requires manual confirmation.

Pause freezes the active interval and overall Pomodoro elapsed progress. **Skip Work** saves the elapsed partial work segment and enters break; **Skip Break** ends the break and waits for confirmation to start work. Stopping Pomodoro early saves all completed and partial segments, displays the summary, and queues the normal completion description. Break segments are stored separately and excluded from normal work/study totals; each Pomodoro start asks whether stored breaks count toward billable hours. Windows notifications are on and sound is off by default.

### 6.5 Session-completion description

- **FR-050:** Whenever a tracked Session finishes, manually or automatically, the system shall show an in-app completion dialog asking what was completed.
- **FR-051:** After time is added manually, the system shall show the same completion dialog for the new Session.
- **FR-052:** The completion description shall be optional free-form text.
- **FR-053:** Pressing `Enter` shall save the current description, including an empty description, and close the completion view.
- **FR-054:** Pressing `Shift+Enter` in the description field shall insert a line break without saving or closing.
- **FR-055:** An empty submission shall save the Session without description text.
- **FR-056:** The saved description shall be attached atomically to the exact Session that opened the completion workflow.

When completion occurs while hidden, Thyme-Me shall show a Windows notification. Activating it restores the in-app completion view. Multiple pending descriptions are queued oldest-first and shown sequentially. Quitting with pending descriptions is allowed; the queue is restored next launch without silently filling descriptions. Saved descriptions remain editable in History. The implementation must not substitute an independent top-level dialog for the required in-app completion view.

### 6.6 Manual entry, history, filtering, and sorting

- **FR-060:** The user shall be able to add a manual Session with any valid association or no association.
- **FR-061:** A global Category shall be selectable for manual time without a Stream.
- **FR-062:** The user shall be able to edit and delete a specific historical Session.
- **FR-063:** A destructive action shall identify its target and effect before irreversible data loss.
- **FR-064:** The application shall provide a built-in Session-history view containing tracked and manual Sessions.
- **FR-065:** History shall be filterable by Stream, Category, Project, duration range, and date or date range.
- **FR-066:** History shall also support filtering by billable status and Session origin when those dimensions contain data.
- **FR-067:** History shall be sortable in ascending or descending order by date, duration, and relevant displayed names, including Stream, Category, or Project name.
- **FR-068:** Each history row shall display its Session date and duration.
- **FR-069:** Multiple active filters shall use logical OR within one dimension and logical AND between dimensions.
- **FR-070:** The user shall be able to quickly create a new manual Session from a previous or commonly used manual entry.
- **FR-071:** Recreating a manual entry shall create a new Session and shall never modify the source Session.
- **FR-072:** Recreating a manual entry shall prefill reusable associations while requiring a new date and time/duration.
- **FR-073:** History shall provide a dedicated **Recently Deleted** view from which a deleted Session can be restored until delayed purge completes.
- **FR-074:** Activating a visible Delete action shall request confirmation; pressing `Shift+Delete` for the selected Session shall skip that confirmation while still moving the Session to Recently Deleted.
- **FR-075:** History shall expose persistent row-selection checkboxes and show the selected count and signed selected duration when one or more rows are selected.
- **FR-076:** History shall use a virtualized continuous list rather than numbered pagination or a Load More boundary.
- **FR-077:** History rows shall support arrow-key navigation, Enter to open details, and Delete to invoke the normal confirmed deletion flow.
- **FR-078:** History column widths shall be draggable and saved automatically; columns shall retain their specified order, and double-clicking a column divider shall reset that column to its default width.
- **FR-079:** At narrower supported widths, lower-priority History columns shall hide automatically rather than force ordinary horizontal scrolling.

History supports free-text search across descriptions and Stream, Category, and Project names; defaults to newest first; and permits editing timestamps, date, associations, description, billability, wage, and rounding. A normal visible Delete action requires confirmation. `Shift+Delete` bypasses that confirmation but never bypasses the recoverable Recently Deleted stage. The timeline is initially read-only and editing occurs through History.

The History page is a continuous table with an optional right inspector, closed until a single-click row selection opens it. The inspector width is draggable and saved. A single dense header row contains title/filtered summary, persistent search, date navigation/range, quick Today/Week/Month/All presets, compact filter popovers, More Filters, and actions. Active filters appear as removable chips. Results default to All time and newest first, group beneath sticky date headings, and show each day's total duration.

Default column order is selection, time, duration, Stream, Category, Project, description, origin, row actions. Origin uses an icon; the date is represented by the group heading. Description is a one-line preview, multiple Categories show the first plus `+N`, and the effective duration is primary while raw duration and the applied rounding rule remain in the inspector. Negative adjustments use a signed duration, adjustment icon, and subdued warning color. Billing fields stay hidden unless billing is enabled or filtered; earnings are an optional column. Columns resize but do not reorder, and low-priority columns hide at narrow widths.

Hover/focus reveals compact row actions plus a More menu, while Delete remains visibly available. Editing, Continue, quick manual recreation, and the expandable foreground-application breakdown live in the inspector. No-results states identify active filters and offer Clear Filters. The persistent bottom transport remains present on History.

A negative correction is stored as a separate signed adjustment record with local date, associations, description, and optional billing values rather than mutating another Session. Adjustments can make a displayed total negative; negative totals and records are visibly identified. Duration filters use raw duration by default and provide an explicit raw/rounded selector.

### 6.7 Billable time and hourly wage

- **FR-080:** A Session shall support billable or non-billable status.
- **FR-081:** Scoped Categories shall provide the default billable status for new Sessions and shall be configurable from the relevant Stream workflow.
- **FR-082:** A scoped Category shall support an optional hourly wage for billable time.
- **FR-083:** A billable Session shall remain valid without an hourly wage.
- **FR-084:** Billability and wage shall remain optional for school and personal use.
- **FR-085:** A saved Session shall preserve the billable status and wage applied when it was created so later default changes do not silently rewrite history.

Categories define the default billable/non-billable state for new Sessions. Each Session may override that state when started. When multiple selected Categories disagree on billability, hourly wage, or currency, the start dialog requires the user to resolve the conflicting values before the Session can start; it never chooses a Category by selection order. Multiple currencies are supported at every applicable level. Thyme-Me calculates estimated earnings only and does not invoice. Each Session's earning is calculated with decimal arithmetic from its effective rounded billable duration and historical hourly wage, then permanently rounded to that currency's minor unit using banker's rounding; aggregates sum these stored per-Session amounts. Historical Sessions preserve their effective billability, wage, currency, rounding inputs, and resulting rounded earning.

### 6.8 Idle, display, sleep, and screen-saver behavior

- **FR-090:** Settings shall provide a toggle controlling automatic idle/display interruption handling. It is on by default, subject to the user's first-run selection.
- **FR-091:** When that setting is enabled, a Session is active, and Thyme-Me is the foreground application, Thyme-Me shall request that Windows prevent automatic system sleep and automatic monitor power-off.
- **FR-092:** When feasible on the selected Windows framework, Thyme-Me shall also prevent automatic screen-saver activation while it is the foreground application and the setting is enabled.
- **FR-093:** Thyme-Me shall never block an explicit user request to sleep, hibernate, lock, sign out, shut down, or turn off the display.
- **FR-094:** If Windows reports display-off, screen-saver/lock, or system-suspend state during an active Session, and the setting is enabled, Thyme-Me shall stop the Session at the detected transition rather than count the inactive interval.
- **FR-095:** On the next detected user activity or resume, Thyme-Me shall issue a Windows notification explaining that the Session stopped and provide a route to the pending completion workflow.
- **FR-096:** The stopped Session shall be persisted even when the main window is hidden.
- **FR-097:** Preventing automatic sleep/display-off shall be active only while the conditions in FR-091 hold and shall be cleared immediately when those conditions stop holding.

When enabled, a configurable generic keyboard/mouse inactivity threshold also stops a Session and defaults to 10 minutes. Display off, screen saver, lock, and suspend are individually configurable. The default stop timestamp is Thyme-Me's detected transition/stop time. After resume/activity, Thyme-Me notifies the user and opens the pending completion workflow when the notification is activated.

Windows exposes system/display power notifications and wake-prevention APIs, but the wake-prevention API does not itself suppress screen savers. Exact screen-saver and session-lock detection must be validated against the chosen framework and supported Windows versions.

### 6.9 Window, notification-area, and keyboard behavior

- **FR-100:** Closing the main window while a Session is active shall hide the window, keep timing, and leave Thyme-Me accessible in the Windows notification area.
- **FR-101:** Closing the main window when no Session or completion interaction is active shall exit Thyme-Me.
- **FR-102:** The notification-area representation shall communicate elapsed Session time as closely as practical within Windows icon-size and refresh constraints.
- **FR-103:** The user shall be able to restore the main window from the notification area.
- **FR-104:** When a Session ends while the main window is hidden, Thyme-Me shall persist the elapsed result and remain able to complete the description workflow.
- **FR-105:** `Ctrl+W` shall invoke the same state-dependent close-window behavior as the window close control: hide to the notification area when work is active or pending, otherwise exit.
- **FR-106:** Thyme-Me shall expose only documented keyboard behavior. Contextual navigation, search, timeline zoom, accessibility, layout, close, and quit shortcuts are permitted; hidden global action shortcuts are prohibited.

The notification-area icon shall show compact elapsed time in `h:mm` where legible and exact time in its tooltip. `Ctrl+Q` is the explicit-quit command and confirms when a Session is active. The window close control, `Ctrl+W`, and `Alt+F4` all use the same state-dependent close behavior: hide while a Session or completion workflow is active or pending, otherwise exit.

### 6.10 Calendar timeline and Stats page

- **FR-110:** The application shall provide a Stats page containing the built-in calendar timeline and summary views.
- **FR-111:** The timeline shall visualize Sessions as time blocks positioned by time of day and grouped by calendar day.
- **FR-112:** The timeline shall show duration spent per day and across the hours of a day.
- **FR-113:** The timeline shall work entirely from local Thyme-Me data without requiring Google Calendar or another service.
- **FR-114:** The Stats page shall provide weekly and monthly summaries.
- **FR-115:** Summary dimensions shall include Stream, Project, Category, and billable versus non-billable time when applicable.
- **FR-116:** The user shall be able to choose the dimension used for a summary visualization.
- **FR-117:** The Stats page shall include relevant totals and graphs, including pie or donut charts where proportional breakdowns are meaningful.
- **FR-118:** Summary and timeline calculations shall respect the selected date range and documented rounding basis.
- **FR-119:** Stats shall use raw actual duration by default and shall provide an explicit Effective/Rounded basis for users who want stored rounded values. Billing and earnings calculations shall continue using effective rounded duration.

Stats defaults to the most recent four months and provides previous/next, week, month, and custom-range controls. Its summary page uses five headline values: lifetime total, selected-range total, daily average, longest Session, and Session count; time values use exact `hh:mm:ss`. The dominant activity area initially shows that four-month range as a daily calendar heatmap, with Daily, Weekly, Cumulative, and Timeline views. Weekly uses seven-day stacked bars; Cumulative uses a cumulative-time line chart. The user's Sunday/Monday week-start choice comes from the first-run wizard.

The heatmap measures the selected raw/effective Stats basis per day, defaults to raw actual duration, scales intensity relative to the greatest visible day, uses the global accent unless exactly one Stream filter is active, and follows the current Stats range. Empty days remain faintly visible. Month and sparse weekday labels follow the configured week start. Hover shows date, raw and effective time, Session count, and top Stream. Timeline geometry always uses raw timestamps and pause intervals even when summaries use the Effective/Rounded basis. Selecting a day opens a right inspector containing that day's Sessions and a Stream pie breakdown; double-click has no separate action. The activity area has a fixed height.

Timeline is a read-only seven-day view inside the fixed Stats activity region. It shows days as columns and time vertically, opening on the week containing the latest selected day. Week data navigates by previous/next week; choosing an individual day uses a month-calendar picker. Visible hours automatically fit the earliest and latest Session with padding, with a Show 24 Hours toggle. Day headings remain fixed while time scrolls; each shows weekday, date, and daily total. Narrow windows preserve readable day widths with horizontal scrolling.

The default time grid uses hour lines only. Visible zoom controls, `Ctrl+wheel`, and precision-touchpad two-finger pinch/expand adjust the time scale; platform pinch events routed as `Ctrl+wheel` use the same zoom path. Today has a horizontal current-time line. Session blocks use low-alpha Stream colors and show Stream, duration, truncated description, and an origin icon. Categories and Project remain in the tooltip and inspector. Overlaps render side by side. Cross-midnight Sessions split at midnight with continuation indicators, and paused portions split the block with a connected gap.

Stored Pomodoro breaks appear as a labeled light shade of the related work-block color. The five-minute transition buffer uses the same light-shade transition treatment with an explicit **Buffer** label while retaining its work-time semantics. Negative adjustments appear as signed markers at their recorded time. Selecting a block opens the standard right Session inspector with expandable application breakdown; double-click opens that Session in History. Selecting a day heading opens the day Stream-pie inspector. Selecting empty timeline space begins a manual Session entry at that time. Filters dim nonmatching Sessions rather than removing them, empty days remain visible with **No Sessions**, and editing remains in History through **Open in History to edit**.

Below activity, equal fixed-width columns show Time insights on the left and rankings/breakdown on the right, stacking at narrow widths. Time insights are quiet label/value rows for most active day, longest Session, daily average, active days, and total Sessions; conditional billable time, per-currency earnings, and relevant budget progress appear without changing the headline strip. Rankings simultaneously show Top Streams and Top Projects, five each with Show All, using color marker, name, duration, and percentage. Clicking a ranking filters the entire page. The right section can switch to a Stream/Project/Category donut breakdown. Global Stream, Category, Project, and billability filters use popovers with removable chips, and Export applies to the filtered Stats range. Summary typography is slightly larger and more spacious than the supplied structural reference.

Stray Sessions appear as **Uncategorized** in breakdowns. Global Categories appear normally in Category views. A Session with multiple Categories contributes once to a **Multiple Categories** segment in additive Category time charts, avoiding duplicated or arbitrary allocation. That segment drills down into Category combinations and constituent Sessions. Stats also provides a clearly labelled, non-additive **Category involvement** view that credits each selected Category with the Session for discovery while warning that its values must not be summed into total time. Earnings totals are grouped by currency and are never combined without explicit exchange-rate data; the initial application performs no currency conversion. For a rounded Session crossing midnight, rounded duration is distributed between local days in proportion to raw duration and the last day absorbs any rounding remainder.

### 6.11 Deferred Google Calendar integration

Google Calendar is not part of the initial implementation. The architecture shall preserve a replaceable integration boundary so a later backend-free Calendar adapter can be added without changing the Session domain, local timeline, or primary data store. Backend-free Google Calendar access is technically feasible using Google's installed/desktop OAuth flow, although a later public OAuth application may require Google verification.

- **FR-120:** No Google Calendar UI, OAuth client, scopes, tokens, cache, or API calls shall ship in the initial implementation.
- **FR-121:** If the future Calendar extension is implemented, it shall use an OAuth 2.0 client registered as a desktop application and complete authorization in the user's system browser.
- **FR-122:** If implemented, the extension shall request the narrowest Google Calendar scope needed for the finalized behavior; write access shall not be requested for a read-only feature.
- **FR-123:** If implemented, OAuth tokens shall be stored locally using an appropriate Windows-protected credential mechanism, not as plain text in the application directory.
- **FR-124:** A future Google Calendar connection shall not require or operate a Thyme-Me backend.
- **FR-125:** If implemented, the user shall be able to disconnect the account and remove locally stored authorization tokens.
- **FR-126:** If implemented, connection failure, token expiry, revoked consent, offline operation, or Google service unavailability shall not prevent local tracking, history, timeline, export, or backup.
- **FR-127:** If implemented, external calendar events and Thyme-Me Sessions shall remain distinguishable in the timeline.

A possible later integration remains constrained to a read-only timeline overlay plus one-way conversion of an event into a manual Session, user-selected calendars, and a labelled offline cache. It may be implemented only while fully client-side and Google-hosted, with no Thyme-Me backend or publisher-operated service. These requirements define an extension seam, not initial-release functionality.

### 6.12 Configurable time rounding

- **FR-130:** Time rounding shall be disabled by default.
- **FR-131:** Settings shall allow the user to select a nearest-minute increment such as 5 or 10 minutes.
- **FR-132:** Each Session shall expose a control to enable/disable or override rounding for that Session.
- **FR-133:** The system shall preserve original start/end timestamps and raw duration even when a rounded duration is used.
- **FR-134:** The system shall store or deterministically derive the rounded duration and the increment/rule that produced it.
- **FR-135:** Changing the global rounding setting shall not silently recalculate historical Sessions.
- **FR-136:** History shall make it possible to distinguish a rounded duration from the raw tracked duration.

Rounding offers 5, 10, 15, and 30-minute presets plus a custom 1–60-minute increment. Midpoints use banker's rounding. For a negative adjustment, the system rounds the absolute duration and restores its negative sign. Billing uses rounded time; Stats defaults to raw actual time and provides an Effective/Rounded basis. History and exports expose both raw and rounded values. The first-run wizard asks whether rounding is enabled and selects its default increment/rule.

### 6.13 Optional foreground-application tracking

- **FR-140:** Foreground-application tracking shall be off by default.
- **FR-141:** Thyme-Me shall begin collecting foreground-application data only after the user explicitly enables the feature through its full disclosure in Settings or the optional Advanced wizard.
- **FR-142:** Enabling the feature shall first explain what is collected, when it is collected, where it is stored, and how it can be deleted.
- **FR-143:** Thyme-Me shall request only non-administrator permissions actually required by the chosen Windows APIs and only as part of this opt-in flow.
- **FR-144:** The initial application shall not request or run with administrator elevation for foreground-application tracking. Intervals whose application identity is inaccessible shall be accumulated under a visible **Unknown elevated application** label.
- **FR-145:** While enabled, application-focus data shall be collected only during an active Session.
- **FR-146:** The baseline collection shall record application identity and accumulated foreground duration; it shall not collect keystrokes, screenshots, clipboard contents, document contents, or browser history.
- **FR-147:** Foreground-application records shall remain local and shall be associated with the exact Session during which they were observed.
- **FR-148:** The user shall be able to disable future collection and delete previously collected application-focus records.
- **FR-149:** The UI shall communicate when application-focus tracking is active.

Tracking stores only application display name and executable filename. It never stores full paths, window titles, or browser domains. The implementation may choose foreground-change events or sampling based on the simplest reliable Windows implementation. At Session completion, the application displays that Session's application-duration breakdown.

Foreground-application tracking pauses during Session pauses and Pomodoro breaks. The first-run disclosure explicitly states that break applications are not collected. Inaccessible elevated applications never trigger elevation; their time is retained in the Session breakdown as **Unknown elevated application** so tracked application totals remain auditable. Deleting a Session deletes its application records as part of the same recovery unit. Timestamp edits clip application intervals to the edited Session bounds, and Settings provides a purge-all activity-data action.

### 6.14 Archiving

- **FR-150:** Streams, Categories, and Projects shall be archivable without deleting their historical Sessions.
- **FR-151:** Archived objects shall be hidden from default active selectors and the main mixer view.
- **FR-152:** Archived objects and their Sessions shall remain available through history filters and an archive-management view.
- **FR-153:** The user shall be able to restore an archived object.
- **FR-154:** Archiving an object shall not change historical Session names, associations, billability, wages, or statistics.
- **FR-155:** If an object is involved in an active Session, the application shall prevent or safely defer its archival.
- **FR-156:** Archive management shall provide All, Streams, Categories, and Projects type tabs, persistent search, compact filters, sorting, persistent row-selection checkboxes, and a saved-width details inspector.
- **FR-157:** Restoring a Stream shall use one focused checklist workflow that can restore compatible archived children while preserving children that were independently archived unless the user explicitly selects them.
- **FR-158:** Restoring a child whose owning Stream remains archived shall include restoration of the owning Stream in the same workflow.
- **FR-159:** The application shall support compatible bulk restore but shall not support bulk permanent deletion of organization objects.

The Archive page defaults to the Streams tab, with All placed before Streams. It uses a comfortable compact list ordered by newest archival by default. Search stays in the header; filters cover type, owning Stream, archived date, and archive reason; sorting also supports name and historical time. Each row shows name, type, owning Stream, archived date, historical time, child count, low-alpha configured color, Restore, and selection. Global Categories appear in a distinct Global section. Scoped Categories and Projects identify their owning Stream. Archived Streams expand inline to show their archived Categories and Projects.

Selecting a row opens a draggable, automatically saved right inspector containing details, archive context, historical totals, children, Restore, View in History, Open Timeline, and a danger section for permanent deletion. Compatible selected rows can be restored in bulk. The Stream restore checklist initially selects children archived as part of that Stream; children independently archived before the parent remain unselected and show an explanation. If restoration requires an archived owner, the same workflow includes that owner. View in History and Open Timeline apply the corresponding object filter.

Archiving a Stream automatically archives its scoped Categories and Projects owned by that Stream. If Sessions from the Stream also use standalone Projects shared with other Streams, the archive workflow lists those shared Projects separately and asks whether each should also be archived. Shared Projects are unselected by default, and the workflow explains that archiving one removes it from active use for every Stream. Archived objects disappear from the main page and active selectors but remain accessible through the timeline, History, archive management, and restore. Attempting to archive an object used by the active Session explains the conflict and offers **Archive after Session stops**. A deferred object shows **Archives when Session stops** and provides Cancel. The Archive empty state explains the feature and links back to Home; the navigation icon has no count badge. At narrow widths, lower-priority columns hide and the inspector overlays the list. Column and inspector widths are draggable and saved. Arrow keys navigate rows, Enter opens the inspector, Space toggles selection, and Delete opens the normal confirmation workflow. The Stream-strip negative/delete-time action asks whether the user wants to select exact Sessions in filtered History or create a negative adjustment.

### 6.15 Time budgets

- **FR-160:** Thyme-Me shall support optional **time budgets** on Streams and Projects without requiring them for ordinary tracking.
- **FR-161:** When configured, the application shall compare the relevant tracked time with its time budget.
- **FR-162:** A time budget shall not prevent tracking after it is reached.

Time budgets are non-recurring by default with optional weekly or monthly reset. An associated Session counts toward both its Stream and Project budgets, including matching historical Sessions recorded before the budget was created. A recurring budget includes all matching records in its current local week or month; a non-recurring budget includes the object's matching lifetime records. Negative adjustments reduce progress, breaks never count, and weekly/monthly boundaries use the configured local week start and calendar month. Time-budget progress uses raw actual duration, while billing continues to use rounded effective duration. There are no budget alerts.

### 6.16 CSV/JSON export, backup, and restore

- **FR-170:** The user shall be able to export Session data as CSV to a user-selected local file.
- **FR-171:** CSV export shall preserve dates, raw timestamps, associations, descriptions, billing values, rounding information, and the documented organization name snapshots.
- **FR-172:** The user shall be able to export JSON whose fields and semantics correspond to the finalized CSV schema, while using appropriate JSON types and nested structures where documented.
- **FR-173:** Export shall not silently include OAuth tokens, credentials, or other secrets.
- **FR-174:** The user shall be able to create a local backup file containing the data and non-secret settings required to restore Thyme-Me.
- **FR-175:** The user shall be able to select and restore a local backup file.
- **FR-176:** Restore shall validate format/version before mutation, explain whether data will be merged or replaced, require confirmation for replacement, and avoid leaving the live store partially restored.
- **FR-177:** Backup and restore shall not require a network connection.
- **FR-178:** Foreground-application data shall be excluded from CSV and JSON exports unless the user explicitly enables **Include application breakdown**, which is off by default. Full backups shall include it for complete restoration after disclosing that inclusion.

CSV uses UTF-8, comma delimiters, RFC 4180 quoting, ISO 8601 timestamps, and quoted multiline descriptions. Its minimum columns are `schema_version`, `session_id`, `start_utc`, `end_utc`, `timezone`, `local_date`, `raw_seconds`, `rounded_seconds`, `rounding_increment`, `origin`, `timing_mode`, Stream ID/current name/original-name snapshot, Category IDs/current names/original-name snapshots/scopes, Project ID/current name/original-name snapshot, description, billable, wage, and currency. Multiple Category values are encoded as JSON arrays inside properly quoted CSV cells, so each Session or adjustment remains one row.

A versioned schema also represents pauses, Pomodoro segments, negative adjustments, and foreground-application summaries. JSON uses nested application records. **Include application breakdown** is off by default and requires an explicit choice for each export. When selected, CSV includes a companion applications CSV keyed by `session_id`; it does not place application arrays in the Session CSV. Export offers All and Current Filtered Results. JSON is a versioned document with metadata, lookup objects, and Session records matching CSV semantics. Both formats include a concise data dictionary identifying Thyme-Me and explaining fields so an LLM or analyst can interpret the data.

CSV export is delivered as a ZIP bundle containing `sessions.csv`, `applications.csv` when applicable, and a README/data dictionary describing Thyme-Me, schema version, fields, units, time-zone conventions, and relationships. JSON export stores equivalent metadata and its data dictionary inside the versioned JSON document.

A backup is a versioned ZIP containing structured JSON data, non-secret settings, custom palettes, foreground-application records, checksums, and a data dictionary; OAuth tokens are excluded and there is no application-level encryption. Before creation, the backup workflow discloses that opted-in foreground-application data is included for complete restoration. Before either Replace or Merge mutates live data, Thyme-Me automatically creates a local rollback backup and reports its location. Restore offers Replace and Merge with a preview and defaults to Replace. During Merge, identical stable IDs with identical content are skipped. If the same ID has differing content, the incoming record receives a new ID and all incoming relationships are remapped consistently before the preview is confirmed.

### 6.17 Appearance and color palettes

- **FR-180:** Settings shall provide appearance-mode choices: Light, Dark, Follow Windows, and Scheduled.
- **FR-181:** Scheduled mode shall let the user configure local times at which light and dark mode begin, including a schedule that crosses midnight.
- **FR-182:** The default light and dark appearance shall use a GitHub-inspired palette family.
- **FR-183:** Each palette variant shall define exactly three user-facing primary colors: **Canvas**, **Surface**, and **Accent**.
- **FR-184:** Text, borders, hover/pressed states, selections, charts, and semantic success/warning/error colors shall be derived or separately system-defined so they remain readable; they do not count as user-selected primary colors.
- **FR-185:** Preset palette families shall provide compatible light and dark variants or a documented derived counterpart.
- **FR-186:** The palette selector shall list common presets before more specialized presets.
- **FR-187:** The initial preset order shall be:
  1. GitHub Default — GitHub Light Default / GitHub Dark Default
  2. Codex Plus — Light+ / Dark+
  3. Catppuccin — Latte / Mocha
  4. Gruvbox Medium — Light / Dark
  5. Solarized — Light / Dark
  6. Claude-inspired Warm
  7. Google-inspired Material
  8. Nord-inspired
  9. Dracula-inspired
  10. Monokai-inspired
- **FR-188:** The Codex-derived preset set shall include all default pairs verified in the locally installed Codex application at specification time: GitHub Default, Plus, Catppuccin Latte/Mocha, Gruvbox Medium, and Solarized.
- **FR-189:** The user shall be able to create a custom palette by entering three `#RRGGBB` hex colors for a light variant and three for a dark variant.
- **FR-190:** The user shall be able to import and export a custom palette configuration file containing the palette name, format version, and validated light/dark color triplets.
- **FR-191:** Invalid hex values, missing required colors, duplicate identifiers, or unsupported palette-file versions shall produce a clear error without changing the active palette.
- **FR-192:** Settings shall provide a contrast adjustment with live preview and a warning when text/interactive contrast falls below the selected accessibility threshold. Saving a failing custom palette shall require explicit acknowledgement.
- **FR-193:** Valid appearance changes shall preview, apply, and persist immediately; invalid edits shall not change the active palette.

Official OpenAI documentation does not currently publish a Codex appearance-preset inventory. FR-188 is therefore based on inspection of the installed Codex package version `26.818.2872.0`, not an OpenAI compatibility promise. A later visual-design pass shall define exact preset triplets. Custom colors are configured as plain `#RRGGBB` text in Settings; saved/imported presets use versioned JSON. Low-contrast custom palettes warn but may be saved after explicit acknowledgement. Default and built-in palettes target WCAG 2.2 AA; a user-acknowledged custom palette is an explicit conformance exception. Chart series derive accessible colors from the selected triplet where feasible. Scheduled mode defaults to light at 07:00 and dark at 19:00 and remains adjustable. Derivation details and behavior when a preset lacks a native counterpart remain visual-design work.

### 6.18 Local persistence

- **FR-200:** The system shall persist Streams, Projects, Categories, Sessions, descriptions, billing data, archive state, time budgets, non-secret settings, and enabled optional-feature data locally.
- **FR-201:** Persisted data shall survive normal application and Windows restarts.
- **FR-202:** Mutable application data shall use an appropriate per-user Windows application-data location, not installed-program directories.
- **FR-203:** Writes shall be resilient to interruption and shall not knowingly leave the primary data store partially written.
- **FR-204:** Schema changes shall use an explicit version and migration strategy before a release changes stored data.

The approved persistence method is SQLite through the stable .NET 10-compatible EF Core SQLite provider. Physical representation, migration, transaction, recovery, and dependency-boundary rules are defined in `docs/engineering/TECHNICAL_DESIGN.md`.

### 6.19 First-run setup wizard

- **FR-210:** The first launch shall present a setup wizard before the main workspace.
- **FR-211:** The wizard shall explain and configure user-facing defaults without requiring external accounts or advanced features.
- **FR-212:** The wizard shall include idle/display handling, inactivity behavior and threshold, stop-timestamp behavior, rounding defaults, local week start (Sunday or Monday), optional start with Windows (off by default), and every other default finalized through the dedicated wizard questionnaire.
- **FR-213:** Optional privacy-sensitive features shall be off until their own disclosure and explicit consent step, even when introduced by the wizard.
- **FR-214:** All wizard choices shall remain editable later in Settings.
- **FR-215:** If Thyme-Me closes before basic setup completion, the wizard shall restart at its first page on the next launch; settings already saved by completed pages remain valid and are prefilled. Closing after basic completion while optional Advanced setup is open shall not make setup incomplete.
- **FR-216:** The Welcome page shall provide **Use recommended defaults**, which completes basic setup immediately with documented safe defaults, creates no sample organization, and opens Home. Optional advanced settings remain available later in Settings.

The detailed screen flow, copy requirements, defaults, skip rules, and acceptance criteria are defined in `docs/product/FIRST_RUN_WIZARD.md`. The wizard uses required basic setup followed by optional advanced setup. Basic setup becomes complete when its last required page is saved; optional Advanced may then be entered or skipped without blocking application use. If Thyme-Me closes before basic setup completes, it restarts from the beginning with saved page choices prefilled. It may be rerun later without data loss, offers backup restore first, and saves each completed page immediately. It does not ask the user's purpose. It offers optional MATH 100 and Work sample organization, defaults normal Sessions to an overwrite-on-type 25-minute duration, remembers the last timing mode, and exposes editable remembered Pomodoro defaults. It follows Windows time/accessibility conventions and asks for week start, appearance/custom colors, Windows startup, rounding, optional billing, per-event interruption behavior with a 10-minute inactivity default, and explicit foreground-tracking consent. Completion opens the main page and highlights Start Session. Subsequent onboarding uses hover/focus tooltips rather than a persistent tour.

### 6.20 Installation, instances, and release policy

- **FR-220:** Thyme-Me 1.0.3 shall be distributed as an intentionally unsigned, self-contained x64 portable ZIP through GitHub Releases. Users extract it to a writable folder and may uninstall it by closing the app and deleting that folder; local application data is removed separately through the documented data controls.
- **FR-221:** Only one Thyme-Me instance may run per Windows user at a time; a second launch shall activate the existing instance.
- **FR-222:** Automatic start with Windows shall be available, off by default, and offered in the first-run wizard.
- **FR-223:** The application shall use the approved C# 14/.NET 10 LTS, WinUI 3/Windows App SDK, Generic Host, modular-monolith, SQLite/EF Core, and self-contained portable x64 baseline defined in `docs/engineering/TECHNICAL_DESIGN.md`. MSIX tooling remains available for development and a future signed installer.
- **FR-224:** Thyme-Me first-party code shall use the custom Thyme-Me Source-Available License 1.0. Official binaries are free of charge for permitted personal, educational, and internal noncommercial use. Redistribution, product incorporation, commercial use, and derivative products require prior written permission. The license is source-available and not OSI-approved open source. Third-party components retain their own terms.
- **FR-225:** Thyme-Me 1.0.3 shall use manual updates from stable GitHub Releases. The inactive `IUpdateService` shall report that automatic updates are not configured. A future updater shall verify release authenticity and integrity and shall never replace an active-session binary unsafely.
- **FR-226:** The release pipeline shall build an intentionally unsigned, self-contained x64 portable archive whose extracted root contains only `thymeme.exe`, `LICENSE.txt`, `NOTICE.txt`, and `files`; the fixed application payload and all other support material live below `files`. The GitHub Release shall expose only the ZIP and `LICENSE` beside GitHub-generated source archives, publish the ZIP SHA-256 in the release notes, attest that ZIP through GitHub, bundle third-party notices/provenance/SBOM inside it, retain standalone evidence as a workflow artifact, and disclose that Windows may show Unknown publisher or SmartScreen. Publishing a GitHub Release is an owner-authorized operation.
- **FR-227:** Settings may retain automatic-update controls for the replaceable future adapter, but 1.0.3 shall not imply that automatic checking or downloading works. Manual **Check Now** shall return an explicit not-configured status.

The initial application follows stable GitHub Releases only. When **Check automatically** is enabled, launch triggers a check if at least 24 hours have elapsed since the last check. **Download automatically** is available only while automatic checking is enabled; on an unmetered connection it may download an authenticated update and then ask the user to install it. **Check Now** remains available when automatic checks are disabled. Download and installation are deferred while a Session or completion workflow is active. Preview/prerelease GitHub Releases are ignored.

### 6.21 Interface-design baseline

The detailed interface specification is maintained in `docs/product/INTERFACE.md`. The initial shell opens around 1280×800, remembers later window bounds/maximized state, and enforces a 960×640 minimum. Primary navigation is a non-collapsible, icon-only column that defaults to 64 px, has a draggable width, and provides accessible hover/focus tooltips. Archive is a dedicated primary destination.

Home uses readable vertical mixer channels inspired structurally by the Studio One console, targeting about six visible channels at 1280 px and horizontal scrolling beyond that. It retains aligned repeated zones without copying audio-specific controls. A persistent bottom transport bar owns the selected/active Session configuration and global Start/Pause/Stop actions. Individual channels show only their own Stream time and retain a direct Start Timer action.

The bottom transport is a square-cornered structural shell panel with no external margin. It starts at the primary-navigation boundary, extends to the right and bottom window edges, and uses a straight top divider; the navigation column independently owns the bottom-left corner. The completion-description panel follows the same edge geometry.

Starting from either the bottom transport or a Stream channel always prompts for duration with the remembered `25` selected; Enter accepts it and numeric typing replaces it. During an active normal Session, the requested total duration remains editable and recalculates the scheduled automatic end. Reducing it to at or below elapsed active time stops immediately and begins completion; increasing it extends the end without rewriting elapsed history.

Stats uses the owner-approved activity-dashboard reference hierarchy: a compact full-width headline-metric strip, one dominant full-width activity visualization with a compact view switch, and two balanced lower insight/ranking columns. It adapts this structure to Thyme-Me data rather than copying the reference application's labels. The visual treatment remains restrained and dense, avoids oversized KPI cards, stacks lower columns at narrow widths, and preserves the application-wide navigation, transport, and saved resizing behavior. Finalized metric mappings and timeline placement are defined in `docs/product/INTERFACE.md`.

- **FR-230:** Practical horizontal regions, split panes, and data columns shall provide visible pointer-drag resize handles and a keyboard-accessible resizing equivalent.
- **FR-231:** A width customization shall be persisted automatically when changed and restored on subsequent launches.
- **FR-232:** Dragging any Stream-channel width handle shall change one shared channel width applied to all existing and future Stream channels rather than creating independent per-channel widths.
- **FR-233:** `Ctrl++`, `Ctrl+-`, `Ctrl+0`, `Ctrl+wheel`, and supported pinch gestures shall adjust or reset the whole Thyme-Me interface scale, including text, controls, icons, spacing, and content. Scale shall persist automatically, stay within 80–150%, and re-layout to the current viewport rather than cropping the application.
- **FR-234:** Settings shall provide a Reset Layout command that restores default region widths, data-column widths, shared Stream-channel width, and interface scale.

### 6.22 Settings

- **FR-240:** Settings shall use a responsive card dashboard without an internal section sidebar. **General** shall be the first and default section.
- **FR-241:** Settings cards shall appear in this order: General, Appearance, Timing, Pomodoro, Billing, Activity & Privacy, Windows, Data, Updates, and About.
- **FR-242:** A persistent Settings search shall search every section and show each matching control with its section path.
- **FR-243:** Ordinary valid setting changes shall apply and persist immediately and show a quiet inline **Saved** status.
- **FR-244:** Uncommon controls shall appear in expandable **Advanced** groups whose expansion states persist automatically.
- **FR-245:** At narrow widths, dashboard cards shall stack into one column. Settings shall never use routine horizontal scrolling.
- **FR-246:** The global Session transport shall be absent throughout Settings, including while a Session is active.
- **FR-247:** Appearance shall provide a persistent miniature application preview beside its controls, or stacked above them at narrow widths.
- **FR-248:** Palette selection shall use a compact searchable list with light/dark swatches. Custom editing shall keep hex fields, native color pickers, live preview, and contrast results together.
- **FR-249:** Saved custom palettes shall support Rename, Duplicate, Export JSON, and Delete. Scheduled appearance shall show its light/dark switching times inline.
- **FR-250:** Layout settings shall group navigation width, icon size, shared channel width, saved panel/column widths, and Reset Layout. Reset Layout shall preview affected values and require confirmation.
- **FR-251:** Timing shall group default duration, last-used mode, accepted formats, and completion behavior. Pomodoro shall have its own card for work, break, buffer, break-billing, and notification defaults.
- **FR-252:** Rounding settings shall show live examples of how sample durations round.
- **FR-253:** Billing shall show an overview and reveal detailed controls only while billing is enabled. Category wages remain edited with Categories; Settings shall explain this and provide a shortcut.
- **FR-254:** Regional settings shall group the Windows-derived region, decimal behavior, currency behavior, and local week start.
- **FR-255:** Activity & Privacy shall group its disclosure, foreground tracking, idle/display handling, inaccessible-application behavior, approximate record count/storage size, and Purge All activity data.
- **FR-256:** Windows shall group start with Windows, notification-area behavior, notifications, sound, and window behavior.
- **FR-257:** Updates shall show installed version, last-check status, stable-only policy, separate automatic-check and automatic-download controls, and **Check Now**.
- **FR-258:** Data shall separate Export, Backup, Restore, Recently Deleted, and Reset groups.
- **FR-259:** Recently Deleted shall default to 30-day retention, offer 7/14/30/60/90-day presets plus a custom 1–365-day value, show each deletion deadline, and purge automatically after the deadline.
- **FR-260:** Destructive Settings actions shall appear in danger sections and open focused confirmation dialogs.
- **FR-261:** Reset All Settings shall preview and reset preferences only; it shall preserve all user data.
- **FR-262:** Settings shall allow the user to create either or both MATH 100 and Work samples and rerun the setup wizard while explicitly preserving existing data.
- **FR-263:** No unfinished Google Calendar or Integrations placeholder shall appear in Settings until a supported integration ships.
- **FR-264:** About shall show version, update state, license, repository, acknowledgements, and **Copy Diagnostics**.

Currency entry shall use a searchable named-currency picker rather than asking users to type an ISO code. Canadian dollars and US dollars appear first, with remaining currencies alphabetical by English name. Money remains stored in ISO currency minor units and uses banker's rounding.
- **FR-265:** Copied diagnostics shall contain app version, Windows version, architecture, and a non-sensitive configuration summary. It shall exclude user paths, tracked applications, Session descriptions, secrets, and other user content.
- **FR-266:** Settings shall provide hover/focus tooltips for icons and unfamiliar controls. Arrow keys shall navigate dashboard sections, `Tab` shall navigate controls, and `Ctrl+F` shall focus Settings search.
- **FR-267:** Saved Settings layout state shall include applicable detail-panel widths and Advanced-group expansion state.

The Settings dashboard begins at General and keeps all major sections visible as responsive cards rather than introducing a second navigation rail. Cards may span the available grid when their controls require additional width. Search temporarily emphasizes matching cards and reveals matching Advanced controls while retaining section paths. Destructive actions never inherit the ordinary immediate-save behavior.

### 6.23 Defensive input, failure, and recovery behavior

- **FR-270:** Every user-controlled string, imported/exported file, backup, palette, URI, update response, and future integration payload shall be treated as untrusted input and validated before use.
- **FR-271:** User input shall never be concatenated into SQL, command shells, scripts, executable arguments, XAML/markup, paths, format strings, or regular expressions. SQL shall be parameterized; stored and imported text shall render as text rather than executable content; process launches shall use fixed structured APIs and allowlisted targets.
- **FR-272:** CSV export shall prevent spreadsheet-formula execution from user-controlled text while preserving a reversible representation documented in the packaged data dictionary. JSON exports and backups shall retain the exact original strings.
- **FR-273:** Import, backup, palette, and update inputs shall be checked for supported versions, type/Unicode validity, numeric overflow, lengths, counts, relationship validity, JSON depth, archive entry paths, duplicate/conflicting entries, checksums, expanded size, and compression ratio before mutation or unbounded allocation. Invalid input shall fail without changing live data.
- **FR-274:** Starting or transitioning a timer shall not be reported as successful until the corresponding state is committed durably. Application, process, Windows, or whole-PC failure shall recover from the last committed transition rather than UI ticks or an assumed in-memory state.
- **FR-275:** Thyme-Me shall distinguish clean from unclean shutdown. After an unclean shutdown it shall validate the local store before ordinary mutation, reconstruct active/completion state according to the scheduled-end rules, and avoid duplicating or silently discarding Sessions.
- **FR-276:** A corrupt, unreadable, partially migrated, or incompatible store shall never be silently replaced by a new empty store. Thyme-Me shall preserve the affected store and offer an applicable rollback, restore, or read-only salvage/export path with a clear explanation.
- **FR-277:** Disk-full, access-denied, failed flush/replace, unavailable-device, and similar persistence failures shall not display false success. The application shall preserve the last durable state, show a recoverable error, and support safe bounded retry or exit where possible.
- **FR-278:** Repeated startup failure shall enter a safe recovery mode that suppresses optional adapters and custom appearance, avoids automatic destructive migration, and keeps diagnostics, backup, restore, and recovery access available without deleting user data.
- **FR-279:** Long operations, collections, caches, event queues, network responses, imports, exports, backups, restores, and reports shall be streamed, paged, virtualized, cancellable, size-limited, or backpressured as applicable so user-controlled scale cannot cause unbounded memory growth. A true process-wide memory exhaustion or corrupt-process condition shall terminate for next-launch recovery rather than attempting unsafe continued operation.

The exact defensive limits are versioned implementation constants sized above the supported dataset target and documented where a user may encounter them. Limits must be checked before large allocation. Resource-limit, validation, cancellation, or failure paths leave live data unchanged unless an earlier transaction was already committed. Every termination path releases Windows hooks, notification icons, wake requests, temporary files, database resources, and other acquired handles as far as the operating system permits.

## 7. Quality requirements

- **NFR-001 — Simplicity:** Core tracking must remain understandable without training or enabling advanced features.
- **NFR-002 — Responsiveness:** Starting, stopping, continuing, filtering, and restoring the main window should feel immediate on supported hardware.
- **NFR-003 — Reliability:** A running Session must not depend on the main window remaining visible.
- **NFR-004 — Accessibility:** Default and built-in controls, dialogs, charts, keyboard behavior, and palettes shall follow WCAG 2.2 Level AA principles where applicable to a Windows desktop application. The application shall provide complete keyboard operation, non-color state cues, Windows UI Automation names/roles/states, screen-reader labels, system text scaling, and readable contrast. A custom palette that the user explicitly saves after a failed-contrast warning is an acknowledged exception to palette contrast conformance; keyboard and assistive-technology requirements still apply.
- **NFR-005 — Privacy:** No study record, activity record, or description may leave the device except through a user-requested export, backup, or explicitly authorized Google Calendar operation.
- **NFR-006 — Least privilege:** Thyme-Me must not request administrator elevation or external-account scopes beyond a feature's demonstrated need.
- **NFR-007 — Recoverability:** The design must minimize lost or duplicated Sessions after crash, suspension, restore, or interrupted writes.
- **NFR-008 — Auditability:** Raw timestamps and the rules used to derive rounded/billable values must remain inspectable.
- **NFR-009 — Offline operation:** All core tracking, history, statistics, export, backup, and restore features must work without network access.
- **NFR-010 — Maintainability:** Timing, persistence, reporting, integration, and presentation responsibilities should remain separable and testable.
- **NFR-011 — Input security:** No untrusted value may change query, command, markup, path, formula, or control-flow meaning through injection. Parsing and transformation must be explicit, bounded, and testable.
- **NFR-012 — Crash and power-loss integrity:** Application crashes, forced termination, Windows failure, and sudden power loss must recover from the last durable transition without silent store replacement, partially applied multi-object operations, or duplicate Session completion.
- **NFR-013 — Resource safety:** Memory, file, database, handle, task, queue, network, and CPU use must remain bounded or cancellable for supported datasets and malformed inputs. The UI must remain responsive during long work.
- **NFR-014 — Failure transparency:** Disk, permission, corruption, migration, import, export, backup, restore, and update failures must be reported accurately and must never show a successful state that was not made durable.
- **NFR-015 — Supply-chain integrity:** Release dependencies, update metadata, checksums, archives, SBOMs, and GitHub attestations must be generated or verified through the approved build and release design; an untrusted artifact must never execute as an automatic update. Artifact attestation establishes provenance, not malware safety or a verified Windows publisher.

Thyme-Me 1.x supports currently serviced Windows 11 releases on x64 without an SLA or guaranteed response time. Windows 10 22H2 x64 is a technical-compatibility target only and receives no promise of Windows 10-specific fixes. The 1.0.3 release does not publish ARM64 or x86 packages. Engineering targets are at least 100,000 Sessions/adjustments, 1,000 organization objects, and 1,000,000 accumulated foreground-application records, with ordinary durable commands completing within 200 ms, initial Home/History results within 300 ms, and common Stats/timeline views within one second on the representative SSD-based system defined by the technical test plan.

## 8. Conceptual data model

This section remains the product vocabulary rather than a table-by-table physical schema. The approved physical representation and mapping rules are governed by `docs/engineering/TECHNICAL_DESIGN.md` and committed migrations.

### Stream

- Stable identifier
- Display name
- Optional presentation metadata
- Optional time-budget data
- Active or archived state
- Creation and update timestamps

### Category

- Stable identifier
- Display name
- Optional parent Stream identifier; absent for a global Category
- Scope derived from the parent relationship: Stream-scoped or global
- Default billable/non-billable status for new Sessions
- Optional wage and currency defaults for scoped Categories
- Active or archived state
- Creation and update timestamps

### Project

- Stable identifier
- Display name
- Optional parent Stream identifier; absent for a standalone Project
- Optional time-budget data
- Active or archived state
- Creation and update timestamps

### Session

- Stable identifier
- Start and end timestamps
- Original local time-zone identifier or offset context
- Derived/display calendar date
- Raw duration
- Optional rounded duration, increment, and rule
- Required requested duration for started Sessions; manual records retain their entered duration or start/end values
- Origin: tracked, manual, continued, or recreated
- Timing mode: normal or Pomodoro
- Pomodoro segment type/configuration when applicable
- Optional completion description, including multiple paragraphs
- Optional Stream association with current-name lookup and original-name snapshot
- Zero or more Category associations with scope validation, current-name lookup, and original-name snapshots
- Optional Project association with current-name lookup and original-name snapshot
- Billable status
- Optional historical hourly wage and currency
- Stored estimated earning rounded to the currency minor unit when billable wage data exists
- Completion state, including a pending-description state when needed

### Negative adjustment

- Stable identifier and signed raw duration
- Optional rounded signed duration, increment, and rule
- Local date and time-zone context
- Optional description
- Optional Stream, Category, and Project associations using the same validation and name-snapshot rules as Sessions
- Billable status and optional historical wage/currency
- Origin fixed to `adjustment`
- No active-timer, pause, Pomodoro, completion-pending, or foreground-application state

### Foreground application record

- Stable identifier
- Parent Session identifier
- Application identity
- Foreground interval or accumulated duration
- No captured content in the baseline model

### Appearance palette

- Stable identifier and display name
- Built-in or custom origin
- Format version
- Light Canvas, Surface, and Accent colors
- Dark Canvas, Surface, and Accent colors

Derived totals and visualizations should be calculated from Sessions unless a later performance decision introduces safely maintained aggregates.

## 9. Conceptual system boundaries

- **Desktop UI:** mixer, in-app dialogs, history, Stats, Settings, and archive management.
- **Timing domain:** duration parsing, active state, Pomodoro, continuation, rounding, and completion.
- **Organization domain:** Streams, Categories, Projects, associations, and archival.
- **Lifecycle integration:** window close, notification area, Windows power/display/session events, and wake prevention.
- **Reporting:** timeline, filters, summaries, graphs, billability, time budgets, and export transformations.
- **Optional integrations:** Google OAuth/Calendar and foreground-application observation.
- **Persistence:** local store, migrations, backup, restore, and protected credentials.
- **Appearance:** mode scheduling, palettes, contrast, import/export, and derived colors.

The approved implementation is a C# 14/.NET 10 LTS modular monolith using WinUI 3, the stable Windows App SDK, Generic Host, MVVM, SQLite through EF Core, and self-contained x64 portable release artifacts. `docs/engineering/TECHNICAL_DESIGN.md` defines module/dependency boundaries, timing and persistence methods, Windows adapters, defensive limits, updates, tests, and incremental implementation order.

## 10. External feasibility constraints

### Google Calendar

Google documents OAuth client IDs for desktop applications and local token-based Calendar API access. Desktop loopback OAuth remains supported. Public use may require consent-screen configuration and verification for requested Calendar scopes. Relevant primary documentation:

- [Google Calendar desktop quickstart](https://developers.google.com/workspace/calendar/api/quickstart/python)
- [Google Calendar OAuth scopes](https://developers.google.com/workspace/calendar/api/auth)
- [Google desktop loopback OAuth status](https://developers.google.com/identity/protocols/oauth2/resources/loopback-migration)

### Windows sleep and display state

Windows provides `SetThreadExecutionState` for preventing automatic system sleep/display power-off and `GUID_CONSOLE_DISPLAY_STATE` notifications for monitor state. `SetThreadExecutionState` does not suppress screen savers and cannot prevent explicit user sleep. Relevant primary documentation:

- [SetThreadExecutionState](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate)
- [Windows system sleep criteria](https://learn.microsoft.com/en-us/windows/win32/power/system-sleep-criteria)
- [Windows power-setting GUIDs](https://learn.microsoft.com/en-us/windows/win32/power/power-setting-guids)

## 11. Duration-input baseline

| Input | Meaning |
| --- | --- |
| `45` | 45 minutes |
| `90` | 90 minutes |
| `1h` | 1 hour |
| `1.5h` | 1.5 hours |
| `1h 30m` | 1 hour 30 minutes |
| `90m` | 90 minutes |
| `01:30` | 1 hour 30 minutes |

Leading and trailing whitespace is ignored. Decimal hours accept `.` and the current Windows regional decimal separator. Units are case-insensitive. Mixed hours/minutes and `HH:MM` are accepted as shown above; invalid, zero, or negative requested durations are rejected with examples. Values at or above 24 hours produce a warning but remain startable.

## 12. Verification outline

The eventual implementation must include automated or platform-appropriate checks for:

- Duration parsing and invalid-input handling.
- Pre-start Cancel creating no record and active Stop saving elapsed time.
- Manual, automatic, continued, and recreated Sessions.
- Pomodoro default and adjusted intervals.
- Pomodoro total countdown freezing during manual pauses.
- Completion-dialog `Enter`, `Shift+Enter`, empty description, and pending completion.
- Valid and invalid Stream/Category/Project combinations.
- Session date/time-zone handling, including Sessions crossing midnight or daylight-saving transitions.
- History filter combinations and each sort field/direction.
- Raw versus rounded duration preservation.
- Billing history across rate changes.
- Timeline placement and weekly/monthly aggregation.
- Raw-default versus Effective/Rounded Stats behavior and raw time-budget progress.
- Additive Multiple Categories breakdown and non-additive Category involvement.
- CSV/JSON escaping, time zones, multiline descriptions, and schema compatibility across versions.
- Backup validation, interrupted restore, and version compatibility.
- Archive/restore without historical mutation.
- Optional activity tracking disabled by default, explicit opt-in, data deletion, no elevation, Unknown elevated application intervals, and no capture outside active Sessions.
- Export exclusion of application breakdown by default and disclosed inclusion in full backups.
- If a future Google Calendar adapter is implemented, disconnect/offline/error behavior without affecting local features.
- Power/display events, wake-request cleanup, explicit user sleep, and resume notification.
- Palette validation, schedule transitions, contrast warnings, import failure, and rollback.
- Explicit acknowledgement before saving a low-contrast custom palette.
- Recommended-default wizard fast path and optional-Advanced completion semantics.
- Documented shortcut coverage and rejection of hidden global action shortcuts.
- Persistence and recovery after abnormal termination.
- Parameterized input handling and rejection of SQL, command, markup, path, regular-expression, and spreadsheet-formula injection attempts.
- Malformed/oversized JSON, ZIP traversal and decompression bombs, duplicate/conflicting import identifiers, numeric overflow, and unsupported schemas without live mutation.
- Disk-full, write-denied, corrupted-store, migration-failure, and interrupted atomic-replacement behavior without false success or silent empty-store replacement.
- Forced process termination at timing and data-mutation boundaries, followed by exactly-once recovery from the last committed state.
- Simulated Windows/PC crash or power loss, unclean-shutdown detection, integrity validation, scheduled-end recovery, and safe recovery-mode entry.
- Bounded-memory generated-scale behavior, streaming/paging/virtualization, cancellation, queue backpressure, cache eviction, and deterministic platform-resource cleanup.

Windows lifecycle, notification-area, screen-saver, foreground-application, and display-scaling behavior require tests on every supported Windows version.

## 13. Open decisions and required follow-ups

The functional product baseline is closed for interface-design purposes. Remaining work is intentionally separated by phase:

All functional requirements other than the explicitly deferred Google Calendar extension belong to one initial-release baseline. The owner has declined a reduced MVP or staged product-requirement split; implementation sequencing may be incremental, but the requirements are not divided into separate product milestones. Windows 11 x64 is the supported release platform; Windows 10 22H2 x64 is compatibility-tested only.

1. **Implementation:** build the complete non-Calendar baseline using `docs/engineering/TECHNICAL_DESIGN.md`; exact remaining copy, icon glyphs, and palette triplets may be completed as visual implementation work without reopening confirmed behavior.
2. **Release-owner inputs:** approve the unsigned-release/SmartScreen disclosure, source-available license, release notes, and emergency rollback procedure.
3. **Release engineering:** validate the GitHub Actions workflow, unsigned self-contained x64 portable artifact, SHA-256 checksums, GitHub attestations, SBOM, and clean Windows 11 launch/rendering behavior.
4. **Distribution readiness:** review the custom Thyme-Me Source-Available License 1.0, required copyright notice, third-party notices, provenance manifest, and SBOM.

## 14. Change control

This baseline records product context supplied through 2026-08-25. Confirmed decisions shall not be reopened silently. The 2026-08-25 owner decision explicitly supersedes the earlier signed-MSIX requirement for version 1.0.1 with an unsigned, attested portable GitHub Release. A future signed installer remains permitted but is not represented as part of 1.0.1.
