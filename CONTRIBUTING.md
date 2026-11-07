# Contributing

Thank you for helping improve Agent Skills. Bug reports, documentation fixes, validation improvements, and plugin proposals are all welcome. Follow the [community guidelines](CODE_OF_CONDUCT.md), and use [SECURITY.md](SECURITY.md) for vulnerabilities.

## What to change where

| Area | How to contribute |
| --- | --- |
| Plugin content in `plugins/` | [Open an issue](https://github.com/Cognitive-Forge-AI/agent-skills/issues/new/choose) describing the problem or improvement. Maintainers apply accepted changes upstream and publish them in a new plugin version through a release PR. Direct edits to `plugins/` are not merged: the next release would replace them, and a published version must never change content. |
| Documentation, `tools/`, workflows, templates | Open a focused pull request. |
| New plugins | Open a plugin proposal issue with its purpose, source, license, supported harnesses, and any scripts, hooks, MCP servers, or external services. Plugins follow the [plugin standard](docs/plugin-standard.md). |
| Removing a plugin | Maintainers remove the package and catalog entry directly, then regenerate the views. |

## Local checks

Use Git and the .NET 10 SDK:

```sh
dotnet test tools/AgentSkills.Catalog.Tests
dotnet run --project tools/AgentSkills.Cli -- validate
git diff --check
```

If you edit a workflow, you can also run [`actionlint`](https://github.com/rhysd/actionlint) from the repository root. CI runs it on every pull request.

After editing `marketplace.json`, regenerate the harness views with `dotnet run --project tools/AgentSkills.Cli -- views` and commit them together with the catalog.

Validation rejects links in package paths and unsafe or missing component paths. It also rejects manifests that disagree with the catalog, out-of-date views, and packages without an Apache-2.0 license file, `README.md`, or `SECURITY.md`. Links to non-public Cognitive Forge repositories are rejected too. Use physical paths: links in any path component are rejected, including linked parent directories.

## Pull requests

Keep changes focused and explain the problem, the resulting behavior, and how you validated it. Use a Conventional Commit style title such as `fix: reject unsafe component paths` or `docs: clarify Cursor installation`. A maintainer reviews every change before merging.

By contributing, you confirm that you can distribute your contribution under Apache-2.0. Preserve third-party attribution and identify any separately licensed material.

## Releasing the catalog library

Maintainers publish `CognitiveForge.AgentSkills.Catalog` to nuget.org with trusted publishing:

1. In a pull request, bump `<Version>` in `tools/AgentSkills.Catalog/AgentSkills.Catalog.csproj` and add the changes to [`CHANGELOG.md`](tools/AgentSkills.Catalog/CHANGELOG.md).
2. After it merges, run the **Publish Catalog Package** workflow on `main`. It creates the `catalog-v<version>` tag, then publishes the package. Pushing that tag yourself starts the same flow.

The run stops before tagging or publishing if the commit is not on `main`, or if the version's tag is already on another commit. In the second case, bump `<Version>`. The job summary reports each step and what to do next.

If the push to nuget.org fails, the tag already exists. A 409 conflict means the version is already published, so a re-run cannot succeed: bump `<Version>` instead. For any other failure, use **Re-run failed jobs** so the run stays on the same commit.
