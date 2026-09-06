# Corrective design-conformance audit

Last reviewed: 2026-09-02

This audit reconciles the corrective implementation with the authority order in
`docs/engineering/IMPLEMENTATION_PROMPT.md`: SDD; Interface and First-run Wizard;
Technical Design; Context; Release; `docs/engineering/llm.txt`. It covers every user-reported omission and the
adjacent UI/settings/release contracts that could regress with those changes.

| Design area | Result | Implementation evidence |
| --- | --- | --- |
| Portable navigation | Conformant | The ZIP root is validated to contain only `thymeme.exe`, `LICENSE.txt`, `NOTICE.txt`, and `files`; `src/ThymeMe.Launcher` resolves only `files\app\thymeme.exe`. |
| Release links and startup | Conformant | Unpackaged startup registration targets the root launcher; application assets remain relative to the nested application base directory; the release workflow uploads only the ZIP publicly. |
| Home and whole-interface zoom | Conformant in code | The scaled root is sized to the inverse viewport, Home streams retain both scroll axes, headers reflow, and the transport has wide, two-column, and compact layouts. |
| Large-window bottom bar | Conformant in code | Shell, page host, and transport hosts stretch to the scaled viewport width; the transport outer grid and border stretch without a fixed content width. |
| Wizard completion | Conformant | Finish is guarded against duplicate invocation, validates whole-number fields, reports failures, rolls startup state back on save failure, and transitions only after setup is durable. |
| Live appearance | Conformant | Light, Dark, Follow Windows, and Scheduled modes apply immediately; Windows color and high-contrast changes are observed; window chrome and content update without restart. |
| Palettes | Conformant | Ten ordered built-ins plus custom create/edit/rename, duplicate, import, export, delete, six strict hex values, preview, contrast warning/acknowledgement, SQLite persistence, and backup remapping are implemented. |
| Settings behavior | Conformant for exposed controls | Ordinary controls save immediately with validation and status; exports honor the explicit application-breakdown opt-in; sample creation is idempotent; reset, purge, restore, and wizard rerun are confirmation-safe. |
| Responsive page audit | Conformant in code | Home, History, Stats, Archive, Settings, wizard, transport, and pending-completion controls reflow or scroll at reduced effective widths; fixed-width wizard content was removed. |
| Update controls | Intentionally unavailable | The cards explicitly say automatic updates are not configured and do not imply that disabled toggles work, matching FR-227. |
| Calendar/integrations | Intentionally absent | No Google Calendar placeholder was added, matching FR-263 and the approved local-first scope. |

Automated evidence includes domain palette validation, palette service
create/update/import/export limits, settings JSON round-trip, scheduled appearance
resolution, architecture boundaries, full solution build/tests, formatting, and
portable package-structure validation. A clean Windows 11 machine remains the
required environment for the final release-claim rendering and accessibility
smoke test; this commit does not publish a release or make that compatibility
claim.
