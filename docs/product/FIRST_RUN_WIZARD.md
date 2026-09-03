# Thyme-Me First-Run Setup Wizard

| Field | Value |
| --- | --- |
| Status | Functional baseline |
| Last updated | 2026-08-23 |
| Authority | Supplements `docs/product/SDD.md`; the SDD wins if a conflict is introduced |

## 1. Goals

The wizard configures Thyme-Me's important defaults without forcing optional integrations or organization. It uses a short basic setup followed by an optional advanced section. All choices remain editable in Settings, and the wizard can be run again without erasing Sessions or organization data.

Essential basic setup must be completed unless the user chooses **Use recommended defaults**. Optional Advanced sections may be skipped. Closing before basic setup completes returns to the beginning on the next launch while retaining validated choices from completed pages for prefilling. After basic setup completes, closing during optional Advanced setup does not block application use.

Each page saves its validated choices immediately when the user selects **Next**. Basic setup counts as complete once its last required page has been saved. The user may then finish immediately or continue into optional Advanced setup. The final screen confirms completion rather than presenting a second editable review.

## 2. Navigation and accessibility

- Use a compact basic flow followed by an explicitly optional advanced flow.
- Provide **Back**, **Next**, progress, and **Skip** only on optional pages.
- Support keyboard navigation, Windows text scaling, high-contrast settings, reduced-motion settings, and screen readers automatically.
- Controls with non-obvious behavior show concise tooltips on pointer hover and keyboard focus. Do not show a persistent onboarding checklist or unsolicited page-tour overlays.
- If setup is rerun, prefill current settings. Applying changed settings must not erase or rewrite historical data.

## 3. Basic setup

### 3.1 Welcome and data source

Explain that Thyme-Me is local-first. Offer **Use recommended defaults**, **Set up step by step**, or **Restore a backup**. Restore follows the SDD's validated preview flow.

**Use recommended defaults** completes basic setup immediately, creates no sample organization, and opens Home with: Normal 25-minute Sessions, remembered Pomodoro 25/5, notifications on, sound off, Windows-derived time/region/week start, Follow Windows appearance, start with Windows off, rounding off, billing off, idle/display handling on with a 10-minute generic inactivity threshold, and foreground-application tracking off. Every value remains editable in Settings.

Do not ask the user's role or purpose and do not personalize defaults based on school, work, research, or personal use.

### 3.2 Starter organization

Offer creation of a first Stream, Categories, and Projects, but do not require any organization before continuing. Explain global versus Stream-scoped Categories only when the user enters Category creation.

Offer two removable, editable samples:

1. **MATH 100**, demonstrating a university-course Stream with example scoped Categories.
2. **Work**, demonstrating a workplace Stream with example scoped Categories and a Project.

The exact example Category/Project names will be finalized with onboarding copy. The samples are real local records, clearly labelled as samples until edited, and can be removed together without affecting user-created data.

### 3.3 Timing defaults

- New normal Sessions default to 25 minutes.
- The duration field initially selects the entire `25`, so typing any digit before pressing Enter replaces it rather than appending to it.
- The start dialog remembers whether Normal or Pomodoro mode was used most recently and selects that mode next time.
- Show Pomodoro work/break settings, initially 25/5, and allow editing. Small muted explanatory text states that changed work/break values will be remembered for future Pomodoro Sessions.
- Windows notifications are on and sound is off by default.
- Time display follows the Windows 12/24-hour regional preference.
- Ask whether the local week begins Sunday or Monday, preselecting the Windows regional convention.

### 3.4 Appearance

Ask for Light, Dark, Follow Windows, or Scheduled appearance. Include live preset previews and direct custom light/dark `#RRGGBB` entry. A custom palette that fails the contrast threshold may be saved only after an explicit warning acknowledgement. If Scheduled is selected, show editable defaults of light at 07:00 and dark at 19:00. Finishing serializes the operation, disables the Finish button while work is active, validates every numeric field, applies startup registration safely, persists the selected palette, and opens Home only after durable settings save succeeds.

Accessibility behavior follows Windows automatically rather than adding a separate accessibility questionnaire.

### 3.5 Windows startup

Ask whether Thyme-Me starts with Windows. Preselect **Off**.

## 4. Optional advanced setup

### 4.1 Rounding

Ask whether rounding is enabled, with **Off** preselected. If enabled, ask for a preset or custom increment. Explain that billing uses rounded time, Stats defaults to raw actual time with an Effective/Rounded option, and History and exports preserve and expose both values.

### 4.2 Billing

Ask whether the user wants billing features. Skipping or declining hides all remaining billing setup.

If enabled:

- Detect currency from the Windows region and allow changing it.
- Offer creation or editing of billable scoped Categories.
- Configure optional hourly wage on scoped Categories within Streams, not as a global wizard wage.
- Explain that a Session may override its Category-derived billability and wage when started.

### 4.3 Idle and interruption handling

Ask whether idle/display interruption handling is enabled, with **On** preselected.

- Let the user enable display off, screen saver, lock, and suspend individually.
- Ask separately whether generic keyboard/mouse inactivity stops a Session.
- When generic inactivity is enabled, default its threshold to 10 minutes and allow editing.
- Use Thyme-Me's detected transition/stop time, not the last input time, as the default stop timestamp.
- Explain that the stopped Session is saved and completion remains pending.

### 4.4 Foreground-application tracking

Explain the feature and allow explicit opt-in, with **Off** preselected. Before enabling, require acknowledgement that collection occurs only during active work intervals, pauses during Session pauses and Pomodoro breaks, and stores only application display name, executable filename, and duration locally. Explicitly state that applications used during Pomodoro breaks are not collected. Also explain deletion and that Thyme-Me never requests elevation; inaccessible elevated applications are grouped as **Unknown elevated application**.

Google Calendar is deferred and does not appear in the initial wizard. The application architecture retains the integration boundary described in the SDD for possible later implementation.

## 5. Completion

The last screen confirms that settings were saved. It does not repeat every selection. **Finish** opens the main page and visually highlights **Start Session** once. Future explanations use hover/focus tooltips rather than a page-tour system.

## 6. Acceptance criteria

- Essential pages cannot be skipped; optional advanced pages can.
- Closing before basic setup completion causes a clean restart from page one with completed-page values prefilled; closing during optional Advanced setup does not make setup incomplete.
- Restore is available before fresh configuration.
- No Stream, Category, Project, billing setup, foreground tracking, or external account is required.
- The 25-minute duration can be replaced with one typing action.
- Regional time, week-start, and currency defaults can all be changed.
- Privacy-sensitive features remain off without explicit consent.
- Rerunning setup preserves all existing user data.
- Completing setup with every optional feature skipped opens the functional main page.
- Choosing **Use recommended defaults** completes basic setup in one action, creates no samples, and opens the same functional main page.
