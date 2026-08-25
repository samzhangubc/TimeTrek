# TimeTrek

TimeTrek is a local-first time tracker for Windows. Organize work into Streams,
start a focused Session in seconds, and see where your time went without sending
your activity to a TimeTrek server.

[Download the latest release](https://github.com/samzhangubc/TimeTrek/releases/latest)

> [!WARNING]
> TimeTrek 1.0.1 is intentionally unsigned. Windows may show **Unknown publisher**
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

TimeTrek stores its working data locally in your Windows application-data area.
It does not include telemetry, advertising, accounts, or cloud sync in version
1.0.1. Use **Settings → Data → Create backup** before moving computers or making
major changes.

## Install and run

TimeTrek 1.0.1 supports Windows 11 on x64 PCs without a support SLA. Windows 10
22H2 x64 is compatibility-only. ARM64, x86, macOS, and Linux builds are not
provided.

1. Open [GitHub Releases](https://github.com/samzhangubc/TimeTrek/releases) and
   download `TimeTrek-1.0.1-win-x64-portable.zip` plus `SHA256SUMS.txt`.
2. Extract the entire ZIP to a writable folder. Do not run the executable from
   inside the compressed archive.
3. Open the extracted folder and run `TimeTrek.App.exe`.
4. If SmartScreen appears, confirm that the file came from this repository. Select
   **More info → Run anyway** only after you are satisfied with its source and
   checksum.
5. Complete the short setup wizard or choose the recommended defaults.

The archive is self-contained: it keeps .NET, Windows App SDK, and supporting
libraries beside TimeTrek. It does not require Developer Mode, administrator
access, a certificate installation, or a system-wide .NET installation.

To uninstall, close TimeTrek from its notification-area menu and delete the
extracted program folder. Export or back up your data first; deleting the program
folder does not automatically remove TimeTrek's separate local application data.

## Verify a download

From PowerShell in the folder containing the download:

```powershell
Get-FileHash .\TimeTrek-1.0.1-win-x64-portable.zip -Algorithm SHA256
```

Compare the result with `SHA256SUMS.txt` on the same GitHub Release. If you use the
GitHub CLI, you can also verify which repository and workflow produced the archive:

```powershell
gh attestation verify .\TimeTrek-1.0.1-win-x64-portable.zip --repo samzhangubc/TimeTrek
```

An attestation proves build provenance; it is not a malware guarantee or a Windows
publisher signature.

## First Session

1. On Home, create or choose a Stream such as a course, job, or hobby.
2. Choose a duration and optional Categories or Project in the bottom transport.
3. Select **Start**. The transport and notification-area icon show active timing.
4. Pause or resume as needed, then select **Stop**.
5. Enter what you completed and press Enter to save it to History.

If the computer or app stops unexpectedly, reopen TimeTrek and follow the recovery
prompt. Durable local data is treated as the recovery authority.

## Updates and support

Version 1.0.1 uses manual updates. Check the
[Releases page](https://github.com/samzhangubc/TimeTrek/releases) for newer stable
versions; the in-app automatic updater is not configured yet.

For reproducible problems, open a
[GitHub issue](https://github.com/samzhangubc/TimeTrek/issues) with the TimeTrek
version, Windows version, steps to reproduce, and diagnostics copied from
**Settings → About**. Do not attach a database, backup, or export unless you have
reviewed it for private information. See [support policy](SUPPORT.md).

## Planned: user-owned cloud backup and sync

A future version may offer optional Google Drive backup or synchronization using
the user's own Google account and storage quota. The intended experience is
explicit opt-in, a standard Google consent screen, clear last-sync/error status,
manual disconnect, encrypted transport, bounded retries, and conflict-safe local
recovery—without a TimeTrek-hosted account or paid TimeTrek server. This feature is
only a development plan: version 1.0.1 contains no Google login or cloud-sync code.

## License and notices

TimeTrek is source-available, not OSI-approved open-source software. Official
binaries are free for permitted personal, educational, and internal noncommercial
use. Redistribution, use in another product, commercial use, and derivative
products require prior written permission. Read [LICENSE](LICENSE), [NOTICE](NOTICE),
and [third-party notices](THIRD_PARTY_NOTICES.md) before redistributing or modifying
the software.

Developer and maintainer documentation is under [`docs/`](docs), beginning with
the [code map](docs/CODE_MAP.md) and [technical design](docs/TECHNICAL_DESIGN.md).
