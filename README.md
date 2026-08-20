# TimeTrek

TimeTrek is a planned Windows desktop application that helps students understand how much time they commit to each course. Its main workspace is inspired by a digital audio workstation (DAW) mixer: every course occupies a simple vertical strip with its key information and time controls.

> [!IMPORTANT]
> TimeTrek is currently in the planning and documentation stage. No application implementation has been selected or started yet.

## Product goals

- Make starting and stopping a study timer quick and unobtrusive.
- Show courses side by side so their time data is easy to compare.
- Support manually adding and deleting recorded time.
- Keep an active timer available from the Windows notification area when the main window is closed.
- Store application data locally like a conventional desktop program.

## Planned experience

The home screen will use one vertical channel per course. Each channel will show that course's main time data and controls to start timing, finish timing, add time, and delete time.

Starting a session will open an in-app dialog rather than another operating-system window. The duration field will accept minutes by default and an `h` suffix for hours (for example, `45` or `1.5h`). A bounded session will stop automatically when its duration is reached.

While a timer is active, closing the main window should leave TimeTrek available in the Windows notification area. The icon should communicate elapsed session time as far as Windows platform constraints allow. If no timer is active, closing the window should exit the application.

## Documentation

- [Software Development Document](docs/SDD.md) — current functional and technical baseline
- [Project Context](docs/CONTEXT.md) — product background, terminology, constraints, and open questions
- [LLM Context](llm.txt) — concise repository context for coding assistants

## Repository status

The product requirements in this repository are an initial baseline and will be refined as more context is provided. Technology choices, data schemas, visual designs, packaging, and release processes remain undecided.

## Contributing

The contribution workflow and project license have not yet been selected. Until they are, use feature branches and pull requests, keep changes focused, and update the context documents whenever a product decision changes.
