# CognitiveForge.AgentSkills.Catalog

Shared rules for Agent Skills plugin marketplaces:

- `CatalogViews` generates the Claude Code, Codex, and Cursor marketplace views from the authoritative root `marketplace.json`. GitHub Copilot reads the root catalog directly.
- `PackageManifests` derives Cursor package manifests from the common plugin manifest.
- `SafePaths` validates relative manifest paths and rejects links in every path component.
- `PackageDigest` computes deterministic package-tree digests.
- `CatalogValidator` checks a marketplace checkout: catalog identity, package paths and manifests, declared components, generated views, and an optional policy (license and URL rules).

The [Agent Skills marketplace](https://github.com/Cognitive-Forge-AI/agent-skills) uses it for validation, and the Cognitive Forge private registry uses it for compilation and public releases. Licensed under Apache-2.0.
