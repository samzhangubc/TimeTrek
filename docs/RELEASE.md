# TimeTrek Release and Automatic-Update Baseline

| Field | Value |
| --- | --- |
| Status | Stack/update architecture approved; production identity and signing pending |
| Last verified | 2026-08-23 |
| Repository | `samzhangubc/TimeTrek` |

## Current repository state

The GitHub repository is public, its default branch is `main`, and GitHub Releases are available. At the verification date it had no published Releases and no GitHub Actions workflows. No GitHub repository feature must be enabled before planning automatic updates.

The implementation stack and updater boundary are approved in `docs/TECHNICAL_DESIGN.md`. A production release workflow cannot be finalized until the real build exists and the MSIX package identity, publisher identity, trusted signing method, and any stable update-metadata location are approved. A placeholder workflow that cannot build, validate, and sign the real application is prohibited.

## Required release design

- Git version tags and GitHub Releases are the authoritative public version channel.
- Release artifacts include signed Windows x64 and ARM64 MSIX packages, cryptographic checksums, and any updater-specific signed metadata.
- The application exposes separate **Check automatically** and **Download automatically** settings. Disabling automatic checks also disables automatic downloads; manual **Check Now** remains available.
- The initial channel includes stable GitHub Releases only; prereleases are ignored.
- On launch, TimeTrek checks when automatic checking is enabled and at least 24 hours have elapsed since its last update check.
- When automatic downloading is enabled, an authenticated update may download on unmetered networks. Installation always requires a user prompt and is deferred while a Session or completion workflow is active.
- Downloaded packages and metadata must be authenticated before installation. An unsigned or invalidly signed update must be rejected with a clear error.
- An update must not terminate or replace TimeTrek while a Session or completion workflow is active. It may download safely and defer installation.
- Failed or interrupted update attempts must leave the installed version usable and preserve the local data store.
- Update metadata must support minimum-compatible versions and prevent accidental downgrade unless an explicit recovery procedure authorizes it.
- Release notes shall be visible before or after installation and link to the corresponding GitHub Release.

## GitHub Actions requirements

The release workflow shall:

1. Trigger only from an owner-authorized version tag or protected manual release action.
2. Check out the exact tagged commit and run required tests, lint, types, and build validation.
3. Build deterministic x64 and ARM64 packages using the finalized stack.
4. Sign packages using a protected signing service or repository/environment secret. No private signing key may be committed to the repository or embedded in ordinary build artifacts.
5. Generate checksums and updater metadata from the signed artifacts.
6. Create a draft GitHub Release and attach all required artifacts.
7. Publish only after all architecture builds and verification checks succeed and the owner authorizes publication.
8. Retain enough provenance to identify the source commit, workflow run, package versions, and signing identity.

Every executable build intended for use must target Windows 10 and Windows 11 on both x64 and ARM64. Architecture builds may run in parallel, but none is deferred to a later product milestone.

Use a protected GitHub Environment for release authorization and signing credentials. Grant the workflow only the minimum repository permissions needed, normally read access to source and scoped write access to release contents during the publish job.

## Decisions required before a signed release

- MSIX identity, publisher identity, and version mapping.
- Code-signing certificate or managed signing service.
- Stable public update metadata location if GitHub Release assets alone cannot provide the validated App Installer-compatible path.
- Retention, rollback, and emergency revocation procedure.

Core implementation and development-signed packaging may proceed before these values exist. The application update coordinator, `IUpdateService` boundary, stable-only GitHub Release checks, bounded download cache, SHA-256 verification, expected-publisher/signature checks, user-confirmed Windows/MSIX installation, and active-Session deferral follow `docs/TECHNICAL_DESIGN.md`. Production identity values must be supplied through protected release configuration rather than source code.
