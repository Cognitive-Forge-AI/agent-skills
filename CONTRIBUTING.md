# Contributing

Thank you for helping improve Agent Skills. Bug reports, documentation fixes, validation improvements, and plugin proposals are all welcome. Follow the [community guidelines](CODE_OF_CONDUCT.md), and use [SECURITY.md](SECURITY.md) for vulnerabilities.

## What to change where

| Area | How to contribute |
| --- | --- |
| Plugin content in `plugins/` | [Open an issue](https://github.com/Cognitive-Forge-AI/agent-skills/issues/new/choose) describing the problem or improvement. Maintainers apply accepted changes upstream and publish them in a new plugin version through a release PR. Direct edits to `plugins/` are not merged: the next release would replace them, and a published version must never change content. |
| Documentation, `tools/`, workflows, templates | Open a focused pull request. |
| New plugins | Open a plugin proposal issue with its purpose, source, license, supported harnesses, and any scripts, hooks, MCP servers, or external services. |
| Removing a plugin | Maintainers remove the package and catalog entry directly, then regenerate the views. |

## Local checks

Use Git and the .NET 10 SDK:

```sh
dotnet test tools/AgentSkills.Catalog.Tests
dotnet run --project tools/AgentSkills.Cli -- validate
git diff --check
```

After editing `marketplace.json`, regenerate the harness views with `dotnet run --project tools/AgentSkills.Cli -- views` and commit them together with the catalog.

Validation rejects links in package paths and unsafe or missing component paths. It also rejects manifests that disagree with the catalog, out-of-date views, and packages without an Apache-2.0 license file. Links to non-public Cognitive Forge repositories are rejected too. Use physical paths: links in any path component are rejected, including linked parent directories.

## Pull requests

Keep changes focused and explain the problem, the resulting behavior, and how you validated it. Use a Conventional Commit style title such as `fix: reject unsafe component paths` or `docs: clarify Cursor installation`. A maintainer reviews every change before merging.

By contributing, you confirm that you can distribute your contribution under Apache-2.0. Preserve third-party attribution and identify any separately licensed material.

## Releasing the catalog library

Maintainers publish `CognitiveForge.AgentSkills.Catalog` to nuget.org by pushing a `catalog-v<version>` tag that matches `<Version>` in `tools/AgentSkills.Catalog/AgentSkills.Catalog.csproj`. The workflow uses nuget.org trusted publishing.
