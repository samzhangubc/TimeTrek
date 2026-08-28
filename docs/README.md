# Thyme-Me documentation

Use this page as the entry point for product, engineering, release, and legal
documentation. Paths in the documents are written from the repository root so
they also work as instructions for coding tools.

## Product

- [Software development document](product/SDD.md) — behavior and requirements;
  this is the product authority.
- [Interface specification](product/INTERFACE.md) — layouts and interactions.
- [First-run wizard](product/FIRST_RUN_WIZARD.md) — onboarding behavior.
- [Product context](product/CONTEXT.md) — decisions, rationale, and constraints.

## Engineering

- [Implementation prompt](engineering/IMPLEMENTATION_PROMPT.md) — self-contained
  implementation and maintenance handoff.
- [Technical design](engineering/TECHNICAL_DESIGN.md) — architecture authority.
- [Code map](engineering/CODE_MAP.md) — feature-to-file ownership index.
- [Machine-readable feature index](engineering/feature-index.json) — compact paths
  for tools and coding assistants.
- [Coding-assistant context](engineering/llm.txt) — condensed repository guardrails.

## Releases and legal

- [Release process](release/RELEASE.md) and [version notes](release/versions/).
- [Notice](legal/NOTICE), [provenance](legal/PROVENANCE.json), and
  [third-party notices](legal/THIRD_PARTY_NOTICES.md).
- The governing source-available license remains at the repository root as
  [LICENSE](../LICENSE).

When documents disagree, follow the product authority first, then the interface
documents, technical design, product context, and coding-assistant context in
that order.
