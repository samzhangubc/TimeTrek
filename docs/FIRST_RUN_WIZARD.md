# TimeTrek First-Run Setup Wizard

| Field | Value |
| --- | --- |
| Status | Functional baseline |
| Last updated | 2026-08-20 |
| Authority | Supplements `docs/SDD.md`; the SDD wins if a conflict is introduced |

## 1. Goals

The wizard configures TimeTrek's important defaults without forcing optional integrations or organization. It uses a short basic setup followed by an optional advanced section. All choices remain editable in Settings, and the wizard can be run again without erasing Sessions or organization data.

Essential setup must be completed. Optional sections may be skipped. Closing the wizard before completion discards wizard progress and restarts it from the beginning on the next launch. A first screen offers **Start fresh** or **Restore a backup**.

Each page saves its validated choices immediately when the user selects **Next**. The final screen confirms completion rather than presenting a second editable review. Setup counts as complete once essential choices have been saved, even if every optional integration is skipped.

## 2. Navigation and accessibility

- Use a compact basic flow followed by an explicitly optional advanced flow.
- Provide **Back**, **Next**, progress, and **Skip** only on optional pages.
- Support keyboard navigation, Windows text scaling, high-contrast settings, reduced-motion settings, and screen readers automatically.
- Controls with non-obvious behavior show concise tooltips on pointer hover and keyboard focus. Do not show a persistent onboarding checklist or unsolicited page-tour overlays.
- If setup is rerun, prefill current settings. Applying changed settings must not erase or rewrite historical data.

## 3. Basic setup

### 3.1 Welcome and data source

Explain that TimeTrek is local-first. Ask whether to start fresh or restore a TimeTrek backup. Restore follows the SDD's validated preview flow.

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

Ask for Light, Dark, Follow Windows, or Scheduled appearance. Include live preset previews and direct custom light/dark `#RRGGBB` entry. If Scheduled is selected, show editable defaults of light at 07:00 and dark at 19:00.

Accessibility behavior follows Windows automatically rather than adding a separate accessibility questionnaire.

### 3.5 Windows startup

Ask whether TimeTrek starts with Windows. Preselect **Off**.

## 4. Optional advanced setup

### 4.1 Rounding

Ask whether rounding is enabled, with **Off** preselected. If enabled, ask for a preset or custom increment. Explain that Stats and enabled billing use rounded time while History and exports preserve and expose both raw and rounded time.

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
- Use TimeTrek's detected transition/stop time, not the last input time, as the default stop timestamp.
- Explain that the stopped Session is saved and completion remains pending.

### 4.4 Foreground-application tracking

Explain the feature and allow explicit opt-in, with **Off** preselected. Before enabling, require acknowledgement that collection occurs only during active work intervals, pauses during Session pauses and Pomodoro breaks, and stores only application display name, executable filename, and duration locally. Explicitly state that applications used during Pomodoro breaks are not collected. Also explain deletion and the possible optional elevation prompt for otherwise unidentified elevated applications.

Google Calendar is deferred and does not appear in the initial wizard. The application architecture retains the integration boundary described in the SDD for possible later implementation.

## 5. Completion

The last screen confirms that settings were saved. It does not repeat every selection. **Finish** opens the main page and visually highlights **Start Session** once. Future explanations use hover/focus tooltips rather than a page-tour system.

## 6. Acceptance criteria

- Essential pages cannot be skipped; optional advanced pages can.
- Closing before completion causes a clean restart from page one without corrupting partially saved settings.
- Restore is available before fresh configuration.
- No Stream, Category, Project, billing setup, foreground tracking, or external account is required.
- The 25-minute duration can be replaced with one typing action.
- Regional time, week-start, and currency defaults can all be changed.
- Privacy-sensitive features remain off without explicit consent.
- Rerunning setup preserves all existing user data.
- Completing setup with every optional feature skipped opens the functional main page.
