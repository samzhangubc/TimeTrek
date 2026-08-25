# Release and update baseline

| Item | Verified state |
| --- | --- |
| Status | Unsigned x64 portable-release workflow implemented; GitHub-hosted validation pending |
| Last verified | 2026-08-25 |
| Repository | `samzhangubc/TimeTrek` |
| Default branch | `main` |
| Public channel | GitHub Releases |

## Release design

TimeTrek 1.0.1 is distributed as a self-contained Windows x64 portable ZIP from
GitHub Releases. It contains `TimeTrek.App.exe`, the .NET runtime, Windows App SDK
runtime files, legal notices, and provenance metadata. It does not install a
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

## Public artifacts

- `TimeTrek-<version>-win-x64-portable.zip`
- `SHA256SUMS.txt`
- `TimeTrek-<version>-sbom.spdx.json`
- GitHub artifact-attestation bundles
- `UNSIGNED_RELEASE.txt`
- `LICENSE`, `NOTICE`, `PROVENANCE.json`, and `THIRD_PARTY_NOTICES.md`

The GitHub attestation establishes which repository, commit, and workflow produced
the archive. It is not a malware audit or a substitute for code signing. Users can
verify it with:

```powershell
gh attestation verify TimeTrek-1.0.1-win-x64-portable.zip --repo samzhangubc/TimeTrek
```

## Platform policy

Windows 11 x64 is supported without an SLA. Windows 10 22H2 x64 is a
technical-compatibility target only, with no promise of Windows 10-specific fixes.
ARM64 and x86 artifacts are not published for 1.0.1. A compatibility claim requires
a clean-machine launch and rendering smoke test; compilation alone is insufficient.

## Updates

Version 1.0.1 uses manual updates from GitHub Releases. The application must not
claim that automatic updating is configured. A future updater may check stable
GitHub Releases and verify the archive checksum and GitHub attestation, but it must
not replace running files or interrupt an active Session. Public-trust code signing
and an installer may be added later without changing the application update
boundary.

## Release procedure

1. Set the source and package version and update `docs/releases/v<version>.md`.
2. Run Release build, tests, formatting verification, and a local portable publish.
3. Commit the exact source to `main` and create the annotated `v<version>` tag.
4. Run the `release` workflow with the matching version input.
5. Confirm the workflow attestation and SHA-256 checksum match the attached archive.
6. On a clean Windows 11 x64 machine, extract the archive, launch TimeTrek, complete
   setup, start/pause/resume/stop a Session, reopen the app, and verify persistence.
7. Record Windows 10 results separately; failure there does not expand support.
8. If a release is bad, remove it from Latest, publish a corrected higher version,
   and retain the previous source/tag and incident notes for provenance.
