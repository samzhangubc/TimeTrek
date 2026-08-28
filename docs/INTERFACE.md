# Thyme-Me Interface Design

| Field | Value |
| --- | --- |
| Status | Active interface-design specification |
| Last updated | 2026-08-24 |
| Authority | Supplements `docs/SDD.md`; functional behavior remains defined by the SDD |

## 1. Design direction

The Home workspace uses the structural idea of the PreSonus Studio One console: adjacent vertical channel strips, repeated stacked control zones, persistent identity per channel, and horizontal navigation across the mixer. Thyme-Me does not imitate audio controls literally. Its strips are substantially wider so Stream names, selectors, actions, timers, and summaries remain easy to read.

Reference: [PreSonus Studio One Console documentation](https://s1manual.presonus.com/en/Content/Mixing_Topics/The_Console.htm).

## 2. Confirmed application shell

- First launch opens centered at approximately 1280×800.
- Subsequent launches restore prior position, size, and maximized state.
- Minimum supported window size is 960×640.
- A non-collapsible, icon-only primary navigation column remains visible throughout the application. It defaults to 64 px but its width is draggable.
- Every navigation icon has an accessible name and a hover/focus tooltip.
- The navigation column contains navigation only. Active-Session status and transport controls live in the persistent bottom transport bar.
- Archive is a dedicated primary navigation destination.
- Home, History, Stats, Archive, and Settings are the finalized primary destinations in that order, with Settings anchored at the bottom. Exact icon glyph and palette treatment remain visual-polish work.
- Practical horizontal regions, split panes, and data columns expose pointer-drag resize handles plus keyboard-accessible resizing. Every customized width is saved immediately and restored automatically.
- `Ctrl++`, `Ctrl+-`, `Ctrl+0`, `Ctrl+wheel`, and supported pinch gestures adjust or reset the entire interface between 80% and 150%. Text, controls, icons, and spacing scale together; the layout is recomputed for the viewport so scaling does not crop the window.
- Settings provides a Reset Layout action for restoring default widths and interface scale.

## 3. Mixer principles

- At the approximately 1280 px first-launch width, the mixer aims to show about six equal-width Stream channels at once. Additional channels scroll horizontally; narrower supported windows show fewer channels without shrinking text below the readable baseline.
- All peer strips use consistent zone ordering so the same control is found at the same vertical position.
- The design favors visible text labels for Stream content and actions; icon-only treatment is reserved for the fixed navigation column and universally recognizable compact utilities with tooltips.
- Horizontal overflow is expected. Wrapping Streams into multiple rows would break the mixer comparison model.
- Each Stream has a user-configurable color. The default uses the basic theme color; the tint covers the channel at low opacity so text and controls remain readable. Meaning cannot depend on color alone.
- The selected and active Streams must remain obvious without causing the strip row to reflow.

## 4. Confirmed Home structure

### Navigation and toolbar

- The navigation column defaults to 64 px wide and remains icon-only and non-collapsible when resized.
- Navigation order is Home, History, Stats, and Archive, with Settings anchored at the bottom.
- Selected navigation uses an accent-filled icon background. All icons expose hover/focus tooltips.
- The Home toolbar contains Search, Add Stream, and Start Unassigned Session.
- Search immediately hides nonmatching Streams and matches Stream, Category, and Project names.
- Start Unassigned opens the normal Session configuration in the bottom transport bar.

### Stream channel

Every channel uses aligned horizontal separators and this top-to-bottom zone order:

1. **Identity:** prominent Stream name at the top; low-alpha user-configurable channel tint.
2. **Timer:** large centered `hh:mm:ss`. When inactive it shows that Stream's total time, not the global active timer.
3. **Controls:** includes a direct Start Timer action so a Session can start from the channel without first selecting it into the transport bar.
4. **Category:** separate full-width multi-select.
5. **Project:** separate full-width single-select.
6. **Summaries and budget.**
7. **Adjustment actions.**

A thin vertical progress rail beside the Timer zone shows bounded-Session progress for the active Stream. It is a time-progress indicator, not an audio level meter.

### Persistent bottom transport bar

The Studio One-inspired bottom transport bar is the authoritative global Session control surface. It remains visible across the primary application pages unless a focused modal workflow explicitly covers it.

- The transport is a structural shell panel, never a floating card: it has square corners and no exterior margin, begins exactly at the navigation-column boundary, and reaches the right and bottom window edges. The navigation column continues independently to the bottom-left corner, so the transport never extends beneath it. A straight top divider separates transport from page content.
- At ordinary width it is a single 88–96 px row. At the 960 px minimum it reflows into two rows rather than hiding or horizontally scrolling controls.
- Its left area shows the `hh:mm:ss` selected/active timer. Its center contains Stream, searchable multi-select Category chips, searchable single-select Project, Normal/Pomodoro segmented mode, and timing configuration. Its right area contains transport controls.
- Before timing starts, Cancel dismisses the start workflow without creating a Session. Once active, the transport provides Pause/Resume and a visually dominant Stop action that saves elapsed time; unavailable actions remain visible but disabled.
- Pomodoro work/break values remain visibly editable in the transport whenever Pomodoro mode is selected.
- Billing, wage, currency, and rounding do not occupy permanent transport controls. Category defaults are managed with Categories, and Session overrides are handled in Session-start settings.
- With no active Session, clicking a Stream selects it and loads that Stream plus its remembered next-Session defaults into the transport bar.
- With no Stream selected, the bar says **Select a Stream or start unassigned** and exposes Start Unassigned.
- The user may adjust Stream (including Unassigned), Categories, Project, mode, and Pomodoro configuration before pressing Start.
- Pressing Start from the transport or directly from a Stream always opens the duration-entry step with the remembered `25` selected. Pressing Enter accepts it immediately; typing any digit replaces the selected value.
- Starting directly from a Stream channel otherwise uses that channel's current next-Session defaults. Its Start control is compact and sits beside the channel timer.
- While a Session is active, the transport bar follows that Session and cannot silently switch its associations merely because another channel is clicked.
- Clicking another Stream while active may select/highlight it for inspection, but transport associations remain locked to the active Session.
- Start controls on other channels are disabled while a Session is active and explain the single-timer rule in a tooltip.
- Each Stream channel continues to display its own Stream timer/total; the global active timer is not duplicated across inactive channels.
- The bar remains visible on Home, History, Stats, and Archive, but not Settings. Focused modal workflows may cover it.
- The completion-description workflow opens immediately above the transport, preserving the stopped timer summary.

### Active duration editing

The requested total duration remains editable while a normal Session is active. It controls the automatic stop boundary:

- increasing it extends the scheduled end;
- decreasing it moves the scheduled end earlier;
- setting it to a value at or below already elapsed active time stops the Session immediately and opens completion;
- edits use the same duration parser and validation as Session start;
- raw elapsed history is never rewritten by changing the requested stop duration.

### Channel state and controls

- Inactive channels show the all-time Stream total labelled **Total**.
- The active channel changes that display to the current Session `hh:mm:ss`; after completion it returns to Total.
- The active channel replaces direct Start with an **Active** state. Pause and Stop remain authoritative in the bottom transport.
- A faint empty Session-progress rail remains on every channel for alignment; the active Stream fills it according to requested-duration progress.
- Summary order is **Total**, **Today**, then **This Week**, each as a vertically aligned labelled row.
- If a time budget exists, a separate vertical budget-progress bar appears, uses raw actual time, and follows the configured daily, weekly, monthly, or total basis. No budget bar is reserved when none is configured.
- Labelled **Add Session** and **Adjust Time** actions occupy the channel bottom.

### Mixer movement and editing

- Channel tint defaults to about 10% opacity and is reduced automatically if needed for contrast.
- A persistent horizontal scrollbar plus touchpad and Shift+wheel input moves through channels beyond the approximately six visible at 1280 px.
- Dragging the width handle on any Stream channel changes the shared width of every Stream channel, including newly created channels; channels never keep independent widths.
- Shared channel width is saved immediately as a local layout preference and restored automatically on later launches.
- Streams reorder by dragging the Identity header; keyboard-accessible Move Left/Right commands provide an equivalent.
- Add Stream is available in the Home toolbar and as a terminal Add Stream channel.
- A Stream header menu opens a right-side inspector while the mixer remains visible.

## 5. Confirmed History structure

### Page frame and controls

- History uses a continuous virtualized Session table with an optional right-side inspector. The inspector is closed initially, opens on single-click row selection, and has a draggable automatically saved width.
- The page uses one dense header row rather than a permanent filter sidebar. It contains the **History** title, filtered Session count and total duration, persistent search, back/forward date navigation, date-range picker, Today/Week/Month/All presets, compact filter popovers, More Filters, and page actions.
- History defaults to All time and newest first. Applied filters appear as removable chips beneath the header.
- Main filters use compact popovers; less common filters remain collapsed beneath More Filters.
- Empty filtered results explain that filters are active and provide **Clear Filters**.
- The global Session transport remains visible at the bottom, following the application-wide transport rules.

### Groups and table

- Rows group beneath sticky calendar-day headings. Each heading shows the regional-format date and that day's total duration.
- Persistent selection checkboxes remain visible. When any rows are selected, the page shows selected count and signed selected duration.
- Rows use comfortable compact density.
- Default column order is: selection, time, duration, Stream, Category, Project, description, origin, row actions. The date comes from the sticky group heading.
- Columns are not reorderable. Each column width is draggable and automatically saved; double-clicking a divider resets that column to its default width.
- Narrow windows automatically hide lower-priority columns instead of forcing routine horizontal scrolling. Hidden values remain available in the inspector.
- Description is a one-line preview. Multiple Categories show the first Category followed by `+N`.
- Effective rounded duration is primary. Raw duration and its rounding rule appear in the inspector.
- Negative adjustments display their signed duration, an adjustment icon, and a subdued warning color.
- Session origin is represented by an accessible icon for tracked, manual, continued, or adjustment records.
- Billing information stays hidden unless billing is enabled or the current filters require it. Earnings is an optional user-enabled column.

### Selection, details, and actions

- Single-click selects a row and opens its details in the right inspector. Arrow keys move row selection and Enter opens/focuses details.
- Hover or keyboard focus reveals compact quick actions and a More menu. Delete remains a visible row action.
- Normal Delete requests confirmation and moves the Session to **Recently Deleted**. `Shift+Delete` skips the confirmation but still uses the recoverable Recently Deleted stage.
- Recently Deleted is a dedicated History subview rather than a temporary Undo notification. It supports restoration until the configured purge deadline, which defaults to 30 days.
- Editing takes place inside the inspector. Continue and quick manual recreation are primary inspector actions.
- Foreground-application breakdown is an expandable inspector section.
- Persistent checkboxes also serve the **Select exact Sessions** flow reached from a Stream adjustment action.

## 6. Stats reference layout

Stats shall use the owner-provided activity-dashboard image as a structural reference, without copying its product-specific labels or data. The reference hierarchy is:

1. one compact, full-width strip of evenly divided headline metrics;
2. one dominant, full-width activity visualization with a compact view switch aligned to its heading;
3. two balanced lower columns: concise Thyme-Me insights on the left and a ranked organizational list on the right.

The result remains restrained and information-dense, but uses slightly larger typography and spacing than the reference. It retains subtle separators, avoids oversized KPI cards, and reserves generous space for the primary activity pattern. The fixed application navigation and persistent bottom transport remain unchanged.

### Header and headline strip

- One compact header contains previous/next period controls, Week/Month/Custom range, a Raw/Effective basis selector that defaults Raw, Stream/Category/Project/billability filter popovers, and Export. Applied filters appear as removable chips.
- The headline strip is one subtly rounded container divided evenly into five exact `hh:mm:ss` values: lifetime total, selected-range total, daily average, longest Session, and Session count.
- Billing never replaces a headline metric. Billable and earnings information appears in the lower insights when applicable.
- Clicking a headline metric changes the dominant activity view to explain that metric.

### Activity area

- The fixed-height dominant area uses a compact **Daily / Weekly / Cumulative / Timeline** view switch aligned with its heading.
- Daily is a calendar heatmap covering the most recent four months. Every cell is one day and encodes the selected raw/effective duration basis using intensity relative to the greatest displayed day; Raw is the first-launch default.
- Weekly is a seven-day stacked-bar view. Cumulative is a cumulative-time line chart. Timeline is the finalized seven-day time-grid view within this activity area.
- The heatmap uses the global accent color, switching to the configured Stream color when exactly one Stream is filtered. Empty days remain as faint neutral cells.
- Month labels and sparse weekday labels follow the configured Sunday/Monday week start.
- Hover shows date, raw and effective time, Session count, and top Stream.
- Single-click selects a day and opens a right inspector with that day's Sessions plus a Stream pie breakdown. Double-click has no separate action.

### Timeline view

- Timeline stays inside the same fixed-height activity area and is read-only. It displays seven days as columns and time vertically, initially choosing the week containing the latest selected day.
- Previous/next navigation moves one week. An individual-day picker uses a month-calendar surface rather than a week picker.
- Visible hours automatically fit the earliest/latest Session with padding. **Show 24 hours** reveals the complete day.
- Day headings remain fixed while the time grid scrolls. Each heading shows weekday, date, and daily total.
- The grid uses hour lines only. Visible +/− controls, `Ctrl+wheel`, and supported precision-touchpad two-finger pinch/expand adjust vertical time scale.
- Narrow windows retain readable day widths and scroll horizontally instead of compressing all seven days.
- Today contains a horizontal current-time line. Empty days remain present with a subtle **No Sessions** label.
- Session blocks use the low-alpha Stream color and show Stream, exact duration, truncated description, and a tracked/manual/continued origin icon. Category and Project appear in tooltip/inspector rather than permanently inside the block.
- Overlapping Sessions appear side by side. Cross-midnight Sessions split at midnight with continuation indicators. Paused portions appear as connected split blocks with a visible gap.
- Stored Pomodoro breaks use a labeled light shade of the related work-block color. The five-minute transition buffer uses the same light transition treatment and a **Buffer** label, but remains work time in calculations.
- Negative adjustments appear as signed markers at their recorded time.
- Selecting a Session opens the standard right inspector with expandable application breakdown. Double-click opens the record in History for editing.
- Selecting a day heading opens the day Stream-pie inspector. Selecting empty grid space begins a manual Session entry at that date/time.
- Filters dim nonmatching Sessions rather than hiding them. Editing remains in History and the inspector exposes **Open in History to edit**.

### Lower insights and rankings

- Two equal fixed-width columns sit beneath activity and stack at narrow widths. This is an explicit exception to the general draggable-width rule; their divider is not resizable.
- **Time insights** uses quiet label/value rows for most active day, longest Session, daily average, active days, and total Sessions.
- When applicable, Time insights also presents billable/non-billable time, earnings grouped by currency, and raw-time budget-progress indicators beside relevant rows.
- The right side simultaneously lists **Top Streams** and **Top Projects**, five each with Show All. Every row shows a color marker, name, exact duration, and percentage.
- Clicking a ranking row filters the entire Stats page to that item.
- The right side also offers a Breakdown view using a Stream/Project/Category donut chart with compact legend.
- Additive Category charts use one **Multiple Categories** segment for multi-Category Sessions. Selecting it drills into Category combinations and constituent Sessions. A separate **Category involvement** view is explicitly non-additive and warns that its values cannot be summed into total time.
- The lower columns are equal rather than user-resizable. The activity height is also fixed; all other global navigation, inspector, and icon-size persistence rules still apply.

## 7. Archive reference layout

Archive is a searchable, compact organization-management page rather than a card gallery. Its persistent header contains title, search, type/owner/date/reason filters, sorting, Recently Deleted, and selection-dependent actions. Type tabs appear in this order: **All, Streams, Categories, Projects**; Streams is selected on first entry. The persistent bottom transport remains visible.

### List and hierarchy

- The list defaults to newest archival first and can sort by name or historical time.
- Rows show persistent selection, low-alpha configured color, name, type, owning Stream, archived date, historical time, child count, Restore, and compact actions.
- Archived Stream rows expand inline to reveal their archived Categories and Projects.
- The Categories tab separates Global Categories into a distinct Global section. Scoped Categories and Projects show their owning Stream beneath the name.
- The list uses comfortable compact density. Column widths are draggable and automatically saved.
- Single-click selects a row and opens a draggable, saved-width right inspector containing details, archive context, historical totals, children, Restore, View in History, Open Timeline, and permanent deletion in a danger section.

### Restore behavior

- Restore is available from both the row and inspector. Compatible checkbox selections support bulk restore.
- Restoring a Stream opens a focused checklist dialog. Children archived by the Stream are selected initially; children that were already independently archived remain unselected and explain why.
- Restoring a Category or Project whose owner is still archived includes the owning Stream in the same restore workflow.
- View in History and Open Timeline navigate to the corresponding page with the object filter already applied.

### Stream archive behavior

- Projects have either one owning Stream or no owner; a Project cannot have multiple owning Streams.
- Archiving a Stream automatically includes its owned Projects and scoped Categories.
- Standalone Projects used by Sessions from that Stream appear in a separate **Shared Projects** checklist. They are unselected by default and explain that archiving them removes them from active use across every Stream.

### Deletion and active-session handling

- Permanent organization deletion is available only for one root object at a time; bulk permanent deletion is not offered.
- Delete opens a focused cascade preview showing affected counts and expandable details. A large cascade additionally requires typing the exact root name.
- The complete deletion unit moves to a dedicated Recently Deleted view shared with deleted Sessions. One Settings-controlled retention period applies to both data types, and restoring organization data restores the complete deleted unit atomically.
- Attempting to archive an object used by the active Session explains the conflict and offers **Archive after Session stops**. Pending rows display **Archives when Session stops** with Cancel.

### Empty, narrow, and keyboard states

- The empty state explains archiving and links back to Home. The Archive navigation icon has no count badge.
- Narrow windows hide lower-priority columns and present the inspector as an overlay.
- Arrow keys navigate rows, Enter opens/focuses the inspector, Space toggles the persistent checkbox, and Delete starts the normal confirmed deletion workflow.

## 8. Settings reference layout

Settings uses a responsive card dashboard, not an internal section sidebar. The normal icon-only primary navigation remains visible with Settings selected, while the bottom Session transport is absent. A persistent header contains **Settings**, global Settings search, the quiet Saved state, and no global Apply button.

Cards appear in this order: **General, Appearance, Timing, Pomodoro, Billing, Activity & Privacy, Windows, Data, Updates, About**. General is first and receives initial keyboard focus. Cards form a readable multi-column dashboard at wide widths and stack into one column narrowly; they never create routine horizontal scrolling. Longer cards may span the grid. Uncommon controls use Advanced disclosure groups, and their expansion state is saved.

### Search, saving, and navigation

- Search covers every control, displays its section path, and reveals matching controls inside collapsed Advanced groups.
- Valid ordinary changes apply and save immediately. A quiet inline **Saved** status confirms persistence.
- Destructive actions always use focused confirmation and never execute through immediate-save behavior.
- Arrow keys move between dashboard cards, `Tab` navigates controls, and `Ctrl+F` focuses Settings search.
- Hover/focus tooltips explain icons and unfamiliar controls.

### Card contents

- **General:** setup-wizard rerun with explicit data-preservation copy, sample-data creation with separate MATH 100 and Work choices, regional/decimal/currency summary, and week start.
- **Appearance:** searchable palette list with light/dark swatches, a persistent miniature application preview, hex fields and color pickers, contrast results, scheduled switching times, and custom-preset actions.
- **Timing:** default duration, last-used mode, accepted formats, completion behavior, rounding controls, and live rounding examples.
- **Pomodoro:** work, break, five-minute buffer, break-billing, and notification defaults.
- **Billing:** enablement overview and progressively revealed details. Category wage editing remains with Categories and is reached through a shortcut.
- **Activity & Privacy:** disclosure, foreground tracking, idle/display handling, the non-elevating **Unknown elevated application** behavior, approximate records/storage, and Purge All.
- **Windows:** startup, notification-area, notification, sound, and window behavior.
- **Data:** distinct Export, Backup, Restore, Recently Deleted, and Reset groups. Export keeps **Include application breakdown** off by default; full Backup discloses that opted-in application records are included. Reset All Settings previews preference changes and preserves data.
- **Updates:** installed version, last check, stable-only policy, separate Check automatically and Download automatically controls, and Check Now.
- **About:** version, update state, license, repository, acknowledgements, and privacy-preserving Copy Diagnostics.

### Recently Deleted and destructive thresholds

- Retention defaults to 30 days and offers 7, 14, 30, 60, and 90 days plus a custom 1–365-day value.
- Each deleted unit shows its purge deadline and purges automatically when that deadline passes.
- A permanent organization cascade is large at five affected organization objects or 25 affected Sessions and then requires the root name.
- No disabled Google Calendar or empty Integrations card appears before a supported integration exists.

### Layout controls and accessibility

- Layout groups navigation width, interface scale, shared channel width, saved panels/columns, and Reset Layout. The reset action previews its scope and confirms.
- Applicable detail-panel widths and Advanced expansion states save automatically.
- Default and built-in Settings presentation follows WCAG 2.2 AA principles where applicable, Windows UI Automation, system text scaling, complete keyboard operation, screen-reader labels, and non-color state cues. A low-contrast custom palette may be saved only after an explicit warning acknowledgement and is treated as a user-selected palette-contrast exception.

## 9. Current design phase

Home, History, Stats summary, Stats Timeline, Archive, and Settings are decision-complete for wireframing. Remaining interface work is focused-dialog visual polish, exact copy, and final palette values.
