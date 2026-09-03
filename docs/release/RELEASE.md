# Release and update baseline

| Item | Verified state |
| --- | --- |
| Status | v1.0.2 release baseline |
| Last verified | 2026-09-02 |
| Repository | `samzhangubc/Thyme-Me` |
| Default branch | `main` |
| Public channel | GitHub Releases |

## Release design

Thyme-Me is distributed as a self-contained Windows x64 portable ZIP from GitHub
Releases. The historical v1.0.1 artifact predates the rename and contains
`TimeTrek.App.exe`; v1.0.2 and later source contains one root entry point named
`thymeme.exe`, while the installed and
public product name is Thyme-Me. The root launcher starts the fixed
`app\thymeme.exe` payload and forwards command-line arguments. The .NET runtime and
Windows App SDK files live under `app`, notices under `legal`, and provenance,
unsigned-build disclosure, and SBOM under `metadata`. It does not install a
system-wide runtime or require Developer Mode. Users extract the complete archive
to a writable folder and launch the executable in place.

The executable is intentionally unsigned. Windows may show **Unknown publisher**
or a Microsoft Defender SmartScreen warning. This limitation must be stated on
the release and in the user README; it must never be described as a verified
publisher build. An unsigned MSIX is not used for public distribution because an
ordinary Windows user cannot install it without separately trusting a certificate.

`.github/workflows/release.yml` is a manual, tag-bound workflow. It restores pinned
dependencies, verifies formatting, builds, runs the test suite, publishes the
self-contained portable folder, generates an SPDX SBOM and SHA-256 checksums,
creates GitHub/Sigstore build-provenance and SBOM attestations, then creates the
GitHub Release only after every gate succeeds. No cloud signing account, private
key, certificate, or production secret is required.

## Portable ZIP layout

```text
thymeme.exe
app/
legal/
metadata/
```

The extracted root has exactly one regular file. The `app`, `legal`, and
`metadata` directory names are a release contract used by the launcher, startup
registration, package validation, and documentation.

## Public artifacts

- `Thyme-Me-<version>-win-x64-portable.zip`
- SHA-256 for that ZIP in the GitHub Release notes

The SBOM, unsigned-build disclosure, license, notices, and provenance are bundled
inside the ZIP. `SHA256SUMS.txt`, a standalone SBOM copy, and GitHub attestation
bundles remain available as workflow evidence rather than additional public
downloads. GitHub's attestation service verifies the public ZIP directly.

The GitHub attestation establishes which repository, commit, and workflow produced
the archive. It is not a malware audit or a substitute for code signing. Users can
verify it with:

```powershell
gh attestation verify Thyme-Me-<version>-win-x64-portable.zip --repo samzhangubc/Thyme-Me
```

## Platform policy

Windows 11 x64 is supported without an SLA. Windows 10 22H2 x64 is a
technical-compatibility target only, with no promise of Windows 10-specific fixes.
ARM64 and x86 artifacts are not published for 1.0.2. A compatibility claim requires
a clean-machine launch and rendering smoke test; compilation alone is insufficient.

## Updates

Version 1.0.2 uses manual updates from GitHub Releases. The application must not
claim that automatic updating is configured. A future updater may check stable
GitHub Releases and verify the archive checksum and GitHub attestation, but it must
not replace running files or interrupt an active Session. Public-trust code signing
and an installer may be added later without changing the application update
boundary.

## Release procedure

1. Set the source and package version and update `docs/release/versions/v<version>.md`.
2. Run Release build, tests, formatting verification, and a local portable publish.
3. Commit the exact source to `main` and create the annotated `v<version>` tag.
4. Run the `release` workflow with the matching version input.
5. Confirm the workflow attestation and SHA-256 checksum match the attached archive.
6. On a clean Windows 11 x64 machine, extract the archive, launch Thyme-Me, complete
   setup, start/pause/resume/stop a Session, reopen the app, and verify persistence.
7. Record Windows 10 results separately; failure there does not expand support.
8. If a release is bad, remove it from Latest, publish a corrected higher version,
   and retain the previous source/tag and incident notes for provenance.
