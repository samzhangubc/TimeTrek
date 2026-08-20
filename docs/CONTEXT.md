# TimeTrek Project Context

## Why this file exists

This is the durable product context for contributors and future development sessions. It separates facts already supplied by the project owner from interpretations and unresolved decisions. Update it when the product direction changes; do not silently turn assumptions into requirements.

## Current product statement

TimeTrek is a Windows desktop application intended to help students track the time they spend studying. It should feel lightweight and simple enough to use throughout a student's time in school.

## Confirmed context

- The initial audience is students.
- Time is organized around individual courses.
- The home screen is inspired by a DAW mixer.
- Each course is displayed in its own vertical column.
- A course column displays its main data and provides controls to start timing, finish timing, add time, and delete time.
- Starting a timer opens a popup inside the application rather than a separate window.
- The popup accepts a duration in minutes by default.
- Appending `h` to the value expresses hours.
- The timer stops automatically when the entered duration is reached.
- If the main window is closed while timing, the application remains on the right side of the Windows taskbar—interpreted provisionally as the notification area/system tray.
- The notification-area icon should show or communicate the elapsed time for the current session.
- If the main window is closed with no active timer, the application exits.
- Information is stored locally, like a conventional desktop application.
- The repository is public and should be maintained in a professional, contributor-friendly form.

## Product principles inferred from the request

These principles are useful interpretations, not direct quotations or final design decisions:

- **Low friction:** a study timer should take very few actions to start or finish.
- **At-a-glance comparison:** the mixer metaphor should make course activity easy to compare horizontally.
- **Low interruption:** timing should continue without requiring the main window to remain open.
- **Local ownership:** study records remain on the user's device in the initial product direction.
- **Honest simplicity:** features and visual density should be constrained to what helps time tracking.

## Terminology

- **Course:** A student-defined subject or class represented by one vertical strip.
- **Course strip:** The mixer-like vertical UI column for one course. "Channel" may be used as an implementation analogy, but user-facing language is not yet chosen.
- **Time session:** A continuous period of study associated with a course.
- **Bounded session:** A session started with a target duration and automatically finished at that duration.
- **Manual time:** Time added by the user instead of captured by a running timer.
- **Main window:** The primary mixer-style application surface.
- **Notification area:** The Windows taskbar area commonly called the system tray. This is the current interpretation of "right side of the task bar."

## UX baseline

The mixer reference describes organization, not audio functionality or a requirement to imitate a specific DAW. The intended structure is a row of narrow, vertical course strips with repeated information and controls. The interface should remain visually simple.

The timer-start popup must be modal within the app's existing surface. It should not create an independent top-level operating-system window. Duration parsing must make simple input fast: a bare value is minutes, while a value ending in `h` is hours.

Closing the main window has state-dependent behavior:

| State at close | Expected result |
| --- | --- |
| A timer is active | Hide the main window; keep tracking; remain accessible in the notification area |
| No timer is active | Exit the application |

## Technical guardrails

- Treat local-only storage as the current privacy boundary. Do not add telemetry, accounts, or remote persistence by assumption.
- Use timestamps as the source of truth for elapsed time; a visual update loop alone is not reliable across sleep, suspension, or process scheduling delays.
- Store mutable user data in a per-user Windows application-data location.
- Plan explicitly for an active timer surviving a hidden window and for recovery after abnormal termination.
- The notification-area timer requirement must be prototyped against actual Windows icon-size and refresh limitations before its exact visual form is promised.
- No technology stack is currently approved.

## Unknowns that need owner input

- Supported Windows versions.
- Technology stack and distribution approach.
- Single versus simultaneous active timers.
- Required versus optional target duration.
- Course fields and summary metrics.
- Course creation, editing, ordering, and deletion flows.
- Time-history views and the exact add/delete behavior.
- Notification and application behavior at automatic completion.
- Accepted duration formats beyond bare minutes and an `h` suffix.
- Local backup, import, export, and reset needs.
- Accessibility target and visual design language.
- Public repository license, contribution process, code of conduct, security policy, and release process.

## Repository conventions at this stage

- Keep product changes traceable through pull requests.
- Keep implementation out of the repository until the initial technology and behavior decisions are made.
- Update `README.md`, `docs/SDD.md`, this file, and `llm.txt` when a decision makes any of them inaccurate.
- Label proposals and assumptions clearly; do not present them as confirmed requirements.
- Never commit secrets, local databases, build output, or machine-specific configuration.

## Context history

### 2026-08-20 — Initial baseline

- Connected the local workspace to `samzhangubc/TimeTrek`.
- Recorded the initial Windows desktop, course mixer, timer, notification-area, and local-storage requirements.
- Left implementation choices open pending additional context.
