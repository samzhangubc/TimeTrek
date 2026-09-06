# Thyme-Me

Thyme-Me is a local-first time tracker for Windows. Organize work into Streams,
start a focused Session in seconds, and see where your time went without sending
your activity to a Thyme-Me server.

[Download the latest release](https://github.com/samzhangubc/Thyme-Me/releases/latest)

> [!WARNING]
> The current 1.0.3 release is intentionally unsigned. Windows may show **Unknown publisher**
> or a Microsoft Defender SmartScreen warning. Download only from this repository's
> Releases page, verify the published SHA-256 checksum or GitHub attestation, and
> do not run a copy from an untrusted source.

## What you can do

- Track one active Session at a time with normal or Pomodoro timing.
- Pause, resume, stop, continue, and manually add or correct recorded time.
- Organize Sessions with Streams, Categories, Projects, colors, and time budgets.
- Review searchable History and recover recently deleted records.
- Explore daily, weekly, monthly, cumulative, and timeline statistics.
- Keep the timer available in the Windows notification area.
- Export JSON or CSV and create restorable local backup archives.
- Optionally record foreground-application names only while a Session is active.
- Adjust the complete interface from 80% to 150% with `Ctrl++`, `Ctrl+-`, or
  `Ctrl+0`.

Thyme-Me stores its working data locally in your Windows application-data area.
It does not include telemetry, advertising, accounts, or cloud sync in version
1.0.3. Use **Settings → Data → Create backup** before moving computers or making
major changes.

## Install and run

Thyme-Me supports Windows 11 on x64 PCs without a support SLA. Windows 10
22H2 x64 is compatibility-only. ARM64, x86, macOS, and Linux builds are not
provided.

1. Open [GitHub Releases](https://github.com/samzhangubc/Thyme-Me/releases) and
   download `Thyme-Me-1.0.3-win-x64-portable.zip`.
2. Extract the entire ZIP to a writable folder. Do not run the executable from
   inside the compressed archive.
3. Open the extracted folder and run `thymeme.exe`.
4. If SmartScreen appears, confirm that the file came from this repository. Select
   **More info → Run anyway** only after you are satisfied with its source and
   checksum.
5. Complete the short setup wizard or choose the recommended defaults.

The archive is self-contained: it keeps .NET, Windows App SDK, and supporting
libraries beside the app. It does not require Developer Mode, administrator
access, a certificate installation, or a system-wide .NET installation.

Releases built from the current source use `Thyme-Me-<version>-win-x64-portable.zip`.
After extraction, the root contains `thymeme.exe`, `LICENSE.txt`, `NOTICE.txt`, and
the `files` folder. Launch the root `thymeme.exe`; all supporting runtime and
metadata files stay under `files`, and the ZIP's checksum is printed in its GitHub Release notes. The
historical v1.0.1 legacy filenames remain documented in its archived release notes.

To uninstall, close Thyme-Me from its notification-area menu and delete the
extracted program folder. Export or back up your data first; deleting the program
folder does not automatically remove Thyme-Me's separate local application data.

## Verify a download

From PowerShell in the folder containing the download:

```powershell
Get-FileHash .\Thyme-Me-1.0.3-win-x64-portable.zip -Algorithm SHA256
```

Compare the result with the SHA-256 printed in the same GitHub Release notes. If you use the
GitHub CLI, you can also verify which repository and workflow produced the archive:

```powershell
gh attestation verify .\Thyme-Me-1.0.3-win-x64-portable.zip --repo samzhangubc/Thyme-Me
```

An attestation proves build provenance; it is not a malware guarantee or a Windows
publisher signature.

## First Session

1. On Home, create or choose a Stream such as a course, job, or hobby.
2. Choose a duration and optional Categories or Project in the bottom transport.
3. Select **Start**. The transport and notification-area icon show active timing.
4. Pause or resume as needed, then select **Stop**.
5. Enter what you completed and press Enter to save it to History.

If the computer or app stops unexpectedly, reopen Thyme-Me and follow the recovery
prompt. Durable local data is treated as the recovery authority.

## Updates and support

Version 1.0.3 uses manual updates. Check the
[Releases page](https://github.com/samzhangubc/Thyme-Me/releases) for newer stable
versions; the in-app automatic updater is not configured yet.

For reproducible problems, open a
[GitHub issue](https://github.com/samzhangubc/Thyme-Me/issues) with the Thyme-Me
version, Windows version, steps to reproduce, and diagnostics copied from
**Settings → About**. Do not attach a database, backup, or export unless you have
reviewed it for private information. See [support policy](.github/SUPPORT.md).

## Planned: user-owned cloud backup and sync

A future version may offer optional Google Drive backup or synchronization using
the user's own Google account and storage quota. The intended experience is
explicit opt-in, a standard Google consent screen, clear last-sync/error status,
manual disconnect, encrypted transport, bounded retries, and conflict-safe local
recovery—without a Thyme-Me-hosted account or paid Thyme-Me server. This feature is
only a development plan: version 1.0.3 contains no Google login or cloud-sync code.

## License and notices

Thyme-Me is source-available, not OSI-approved open-source software. Official
binaries are free for permitted personal, educational, and internal noncommercial
use. Redistribution, use in another product, commercial use, and derivative
products require prior written permission. Read [LICENSE](LICENSE),
[NOTICE](docs/legal/NOTICE), and
[third-party notices](docs/legal/THIRD_PARTY_NOTICES.md) before redistributing or modifying
the software.

Developer and maintainer documentation begins at the
[documentation index](docs/README.md), with direct links to the
[code map](docs/engineering/CODE_MAP.md) and
[technical design](docs/engineering/TECHNICAL_DESIGN.md).

## Repository layout

- [`src/`](src) — application source code, organized by architectural layer.
- [`tests/`](tests) — automated application, domain, infrastructure, and architecture tests.
- [`docs/`](docs) — indexed product, engineering, release, and legal documentation.
- [`.github/`](.github) — contribution guidance and GitHub Actions workflows.
- [`.config/`](.config) — shared development-tool configuration.
