# TimeTrek Software Development Document

| Field | Value |
| --- | --- |
| Document status | Initial requirements baseline |
| Product status | Planning; implementation not started |
| Target platform | Windows desktop |
| Last updated | 2026-08-20 |

## 1. Purpose

This document captures the current software requirements for TimeTrek. It intentionally avoids selecting an implementation stack or inventing behavior that has not yet been specified. Future product and engineering decisions should update this document and `docs/CONTEXT.md` in the same change.

## 2. Product summary

TimeTrek is a local-first time tracker intended primarily for students. It organizes study time by course and presents those courses in parallel, using a home-screen layout inspired by the vertical channel strips of a DAW mixer.

The core interaction should stay simple: select a course, start or finish a timed study session, or correct the stored history by adding or deleting time.

## 3. Scope

### 3.1 In scope for the initial product

- A Windows desktop application.
- Course-based time organization.
- A mixer-style home screen with one vertical strip per course.
- Course summary data within each strip.
- Start, finish, add-time, and delete-time actions for each course.
- An in-app duration prompt when starting a timer.
- Duration input in minutes by default, with an `h` suffix for hours.
- Automatic completion when a specified duration is reached.
- Background presence in the Windows notification area while a timer is active.
- A notification-area icon that communicates elapsed time, subject to platform and accessibility constraints.
- Local persistence of application data.
- Application exit when the main window is closed and no timer is active.

### 3.2 Not yet in scope

The following are not established requirements and must not be assumed without a later decision:

- User accounts, authentication, or cloud synchronization.
- Mobile, web, macOS, or Linux clients.
- Collaboration or shared course data.
- Calendar, learning-management-system, or other third-party integrations.
- Analytics beyond the unspecified "main data" shown for each course.
- Notifications, sounds, or interruption-blocking behavior.

## 4. Users and primary use cases

### 4.1 Primary user

A student who wants a low-friction record of the time spent studying individual courses.

### 4.2 Core use cases

1. The user views courses and their principal time data side by side.
2. The user starts a study session for a course and enters a duration.
3. The application tracks the session and stops it when the entered duration is reached.
4. The user ends a session before its automatic stop time.
5. The user closes the main window while a session continues and monitors it from the notification area.
6. The user manually adds time to a course.
7. The user deletes incorrectly recorded time.
8. The user closes the application when no timer is running.

## 5. Functional requirements

Requirements use stable identifiers so later discussions and tests can refer to them.

### 5.1 Courses and home screen

- **FR-001:** The system shall represent each course as an individual item.
- **FR-002:** The home screen shall display courses as separate vertical strips in a horizontally arranged, mixer-inspired layout.
- **FR-003:** Each course strip shall display the course's main time data.
- **FR-004:** Each course strip shall provide actions to start timing, finish timing, add time, and delete time.

The exact course fields, time summaries, empty state, sorting behavior, and layout overflow behavior remain open.

### 5.2 Timing sessions

- **FR-010:** Starting a timer shall present a dialog within the main application surface, not a separate operating-system window.
- **FR-011:** The start dialog shall accept a duration expressed in minutes when no unit suffix is supplied.
- **FR-012:** The start dialog shall accept an `h` suffix to express a duration in hours.
- **FR-013:** The system shall validate duration input and prevent ambiguous or non-positive values from starting a bounded session.
- **FR-014:** The system shall automatically finish a bounded session when its requested duration is reached.
- **FR-015:** The user shall be able to finish an active session manually.
- **FR-016:** The displayed and persisted elapsed time shall be derived from session timestamps rather than accumulated UI ticks, so sleep or temporary suspension does not silently lose time.

Whether an unbounded stopwatch session is supported, whether more than one course can run at once, and how automatic completion is communicated remain open decisions.

### 5.3 Manual corrections

- **FR-020:** The user shall be able to add time manually to a course.
- **FR-021:** The user shall be able to delete recorded time from a course.
- **FR-022:** Destructive actions shall make their target and effect clear before irreversible data loss.

The unit, granularity, edit model, confirmation behavior, and any undo capability remain open.

### 5.4 Window and notification-area behavior

- **FR-030:** Closing the main window while a timer is active shall keep the timer running and leave the application accessible from the Windows notification area.
- **FR-031:** Closing the main window when no timer is active shall exit the application.
- **FR-032:** While a timer is active, the notification-area representation shall communicate elapsed session time as closely as is practical within Windows icon-size and refresh constraints.
- **FR-033:** The user shall be able to restore the main window from the notification-area representation.
- **FR-034:** When a session ends while the main window is closed, the application shall persist the result and may exit only after any required completion interaction is resolved.

The precise icon treatment, tooltip or context menu, refresh frequency, completion notification, and post-completion lifetime remain open.

### 5.5 Local data

- **FR-040:** The system shall persist course and time-session information locally on the user's computer.
- **FR-041:** Persisted data shall survive normal application restarts and Windows restarts.
- **FR-042:** Application data shall be stored in an appropriate per-user Windows application-data location, not alongside installed program binaries.
- **FR-043:** Writes shall be resilient to interruption and shall not knowingly leave the primary data store partially written.

The storage engine, schema, backup/export features, retention, and data migration strategy remain open.

## 6. Quality requirements

- **NFR-001 — Simplicity:** Common timing actions should be understandable without training.
- **NFR-002 — Responsiveness:** Starting, stopping, and switching between the main window and notification area should feel immediate on supported hardware.
- **NFR-003 — Reliability:** A running session must not depend on the main window remaining open.
- **NFR-004 — Accessibility:** Controls, dialogs, state indicators, and color choices should support keyboard access, readable contrast, and assistive technology available on Windows.
- **NFR-005 — Privacy:** The initial product should operate without transmitting study records off the device unless a later, explicit feature changes this requirement.
- **NFR-006 — Recoverability:** The design should minimize loss or duplication of recorded time after an abnormal exit.
- **NFR-007 — Maintainability:** Product rules, persistence logic, and presentation logic should have separable responsibilities and automated tests where practical.

Specific performance targets and supported Windows versions have not yet been defined.

## 7. Conceptual data model

This is a vocabulary-level model, not a selected database schema.

### Course

- Stable identifier
- Display name
- Optional presentation metadata, pending design
- Creation and update timestamps

### Time session

- Stable identifier
- Associated course identifier
- Start timestamp
- End timestamp, absent while active
- Optional requested duration
- Origin, such as tracked or manually entered

Derived totals should be calculated from session records unless a later performance decision introduces safely maintained aggregates.

## 8. Conceptual system boundaries

The initial design is expected to contain these responsibilities regardless of implementation technology:

- **Desktop UI:** mixer-style course view, dialogs, and history interactions.
- **Timing domain:** duration parsing, active-session state, elapsed-time calculation, and completion rules.
- **Application lifecycle:** main-window close behavior, background lifetime, and notification-area integration.
- **Persistence:** local storage, safe writes, schema evolution, and recovery.

No framework, programming language, database, packaging format, or installer has been selected.

## 9. Duration-input baseline

Examples derived from the current requirement:

| Input | Meaning |
| --- | --- |
| `45` | 45 minutes |
| `90` | 90 minutes |
| `1h` | 1 hour |
| `1.5h` | 1.5 hours, if decimal values are confirmed |

Whitespace tolerance, decimals, mixed units, maximum duration, localization, and error messages require product decisions. The final parser specification and tests must be written before implementation.

## 10. Verification outline

The eventual implementation should include automated checks for:

- Duration parsing and invalid-input handling.
- Manual and automatic session completion.
- Timestamp-based elapsed-time calculation across suspension or delayed UI updates.
- Persistence and recovery of active and completed sessions.
- Main-window close behavior with and without an active timer.
- Adding and deleting time without affecting unrelated courses or sessions.

Windows notification-area behavior and the final layout will also require testing on supported Windows versions and at common display scaling settings.

## 11. Open decisions

1. Which Windows versions and processor architectures will be supported?
2. Which UI framework, language, packaging format, and installer will be used?
3. Can multiple courses have active timers simultaneously?
4. Is a duration required, or can a session run as an open-ended stopwatch?
5. Which "main data" appears on each course strip?
6. How are courses created, edited, ordered, archived, or deleted?
7. What exactly does "delete time" target: the latest session, a selected record, or an entered adjustment?
8. What should happen when a bounded session completes while the main window is hidden?
9. How should elapsed time be represented in or beside the small notification-area icon?
10. How should the application recover a session interrupted by shutdown, crash, sleep, or a clock change?
11. Are data export, import, backup, or reset controls required?
12. What visual identity, accessibility target, license, and contribution policy should the public repository use?

## 12. Change control

This baseline records only the context supplied as of the date above. New context should be reflected in the requirements, open decisions, and `llm.txt`. Requirements should not be treated as implementation-complete until their open UX and technical decisions are resolved and testable acceptance criteria exist.
