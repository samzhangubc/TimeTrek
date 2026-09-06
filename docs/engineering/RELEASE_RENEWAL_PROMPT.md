# Surgical portable-release renewal prompt

Use **GPT-5.4 Mini with low reasoning** for this narrow, well-defined repository
edit. Keep tool output and commentary concise.

Perform a surgical Thyme-Me release-only update. Publish a new immutable patch
release rather than rewriting the existing v1.0.2 tag or release.

The new GitHub Release download area must expose only:

- `Thyme-Me-<version>-win-x64-portable.zip`;
- `LICENSE`;
- GitHub's automatically generated source-code archives.

Do not expose the checksum, SBOM, notice, provenance, or attestation bundles as
additional public release assets. Keep the ZIP checksum in the release notes and
retain the other evidence in the workflow artifact and GitHub attestations.

The extracted portable ZIP root must contain exactly:

```text
thymeme.exe
LICENSE.txt
NOTICE.txt
files/
```

Place everything else below `files/`: the fixed application payload under
`files/app/`, and third-party notices, provenance, unsigned-release disclosure,
and SPDX SBOM under suitable subdirectories. The root launcher must resolve only
the fixed `files\app\thymeme.exe` path relative to its own executable directory,
never from the current working directory or user-controlled input. Correct startup
registration and every directly affected packaging, workflow, checksum, release,
documentation, and application link.

Do not alter application features, UI, data formats, dependencies, architecture,
or unrelated documentation. Preserve historical release facts. Make no cleanup or
refactor outside the requested release structure.

Before publication, verify formatting, Release build, tests, exact ZIP root and
nested-file shape, SHA-256, launcher behavior from an unrelated working directory
and a path containing spaces and non-ASCII characters, and the exact GitHub
Release asset list. Commit and push the minimal change, create an annotated patch
version tag on that exact commit, run the tag-bound release workflow, and verify
the published release and attestations. Do not claim success for a gate that did
not run.
