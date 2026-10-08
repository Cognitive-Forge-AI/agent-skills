# Agent Skills

An open marketplace for agent plugins and skills, published by Cognitive Forge.

Each plugin lives in `plugins/<name>/`. [`marketplace.json`](marketplace.json) is the authoritative catalog; it lists each plugin with its version and description. Your harness's plugin browser shows the same catalog after you add the marketplace. You install plugins with your harness's native commands. This marketplace has no installer of its own.

## Install

Every route below registers the marketplace as `agent-skills` and installs plugins as `<plugin>@agent-skills`. Start a new session, or reload plugins, after installing or updating.

### Claude Code

```text
/plugin marketplace add Cognitive-Forge-AI/agent-skills
/plugin install <plugin>@agent-skills
```

From a shell, use `claude plugin marketplace add Cognitive-Forge-AI/agent-skills` and `claude plugin install <plugin>@agent-skills`.

| Task | Command |
| --- | --- |
| Refresh the catalog | `claude plugin marketplace update agent-skills` |
| Update a plugin | `claude plugin update <plugin>@agent-skills` |
| Pin a tag or branch | `claude plugin marketplace add Cognitive-Forge-AI/agent-skills#<ref>` |
| Uninstall a plugin | `claude plugin uninstall <plugin>@agent-skills` |
| Remove the marketplace | `claude plugin marketplace remove agent-skills` (also uninstalls its plugins) |

### Codex

```sh
codex plugin marketplace add Cognitive-Forge-AI/agent-skills
codex plugin add <plugin>@agent-skills
```

| Task | Command |
| --- | --- |
| Refresh the catalog | `codex plugin marketplace upgrade agent-skills`, then `codex plugin add <plugin>@agent-skills` again to update the installed copy |
| Pin a ref | `codex plugin marketplace add Cognitive-Forge-AI/agent-skills --ref <ref>` |
| Remove a plugin | `codex plugin remove <plugin>@agent-skills` |
| Remove the marketplace | `codex plugin marketplace remove agent-skills` |

You can also browse and install from `/plugins` inside Codex.

### GitHub Copilot CLI

```sh
copilot plugin marketplace add Cognitive-Forge-AI/agent-skills
copilot plugin install <plugin>@agent-skills
```

Refresh with `copilot plugin marketplace update agent-skills` and `copilot plugin update <plugin>`. Pin with `Cognitive-Forge-AI/agent-skills#<ref>`. Remove with `copilot plugin uninstall <plugin>` and `copilot plugin marketplace remove agent-skills`.

### VS Code agent plugins

Add the marketplace to your user `settings.json`, then install from the plugin view or with `/plugin install`:

```json
"chat.plugins.marketplaces": ["Cognitive-Forge-AI/agent-skills"]
```

For a reproducible setup, pin a full commit SHA: `"Cognitive-Forge-AI/agent-skills#<commit-sha>"`.

### Cursor

- **Teams:** an administrator opens **Dashboard → Plugins & MCPs → Team Marketplaces → Add Marketplace → Import from Repo** and pastes `https://github.com/Cognitive-Forge-AI/agent-skills`.
- **Individuals:** copy a plugin directory to `~/.cursor/plugins/local/<plugin>`, then run **Developer: Reload Window**. Enterprise administrators may need to allow local plugins.

This marketplace is not listed in Cursor's public plugin directory.

### OpenCode and other skill-compatible harnesses

OpenCode loads skills, not complete plugins. Copy a skill directory, for example `plugins/<plugin>/skills/<skill>/`, into one of:

- Project: `.opencode/skills/`, `.agents/skills/`, or `.claude/skills/`
- User: `~/.config/opencode/skills/`, `~/.agents/skills/`, or `~/.claude/skills/`

Other harnesses that read `SKILL.md` directories work the same way. The [Vercel `skills` CLI](https://github.com/vercel-labs/skills) is an optional convenience for copying skills; this marketplace does not depend on it. To request support for another harness, [open an issue](https://github.com/Cognitive-Forge-AI/agent-skills/issues/new/choose).

### Component compatibility

Every package ships Claude Code, Codex, and Cursor manifests. Copilot and VS Code read the Claude-format package. Skills are the common denominator and work in every route above. Agents, commands, hooks, MCP servers, and apps are loaded only where a harness supports that component, and they use that harness's own configuration format. Check your harness's plugin documentation before relying on them. Each plugin's README or skill files describe its harness-specific requirements.

## Versions and updates

Every plugin has its own version. The marketplace version (`version` in `marketplace.json`) increases by one minor version with each release PR that changes plugins. A published plugin version never changes content: any change ships under a new version. To reproduce an installation exactly, pin a full commit SHA. A plain `owner/repo` registration follows `main`.

## Trust and permissions

Plugins add instructions to your agent. They can also add scripts, hooks, MCP servers, apps, commands, and agents that run with your agent's permissions. Review the whole package before enabling it, and give the agent access suited to your project. Validation checks packaging, paths, licenses, and catalog consistency. It does not establish that every instruction or executable component is safe for your environment.

If you register more than one marketplace that ships the same plugin, install it from only one of them. Otherwise its skills load twice.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md). Plugin packages are published through reviewed release PRs. Report problems with plugin content in [issues](https://github.com/Cognitive-Forge-AI/agent-skills/issues) rather than editing `plugins/` directly. Pull requests for documentation, validation tooling, and repository infrastructure are welcome. Follow the [community guidelines](CODE_OF_CONDUCT.md), and report vulnerabilities privately as described in [SECURITY.md](SECURITY.md).

## Validation tooling

`tools/` contains the open-source catalog library ([`CognitiveForge.AgentSkills.Catalog`](tools/AgentSkills.Catalog/README.md) on nuget.org) and the `agent-skills` CLI that validates this repository:

```sh
dotnet run --project tools/AgentSkills.Cli -- validate
dotnet run --project tools/AgentSkills.Cli -- views     # regenerate harness catalog views
dotnet test tools/AgentSkills.Catalog.Tests
```

`marketplace.json` is authoritative. `.claude-plugin/marketplace.json`, `.agents/plugins/marketplace.json`, and `.cursor-plugin/marketplace.json` are generated from it. GitHub Copilot reads the root catalog. [`catalog-policy.json`](catalog-policy.json) adds this marketplace's rules: Apache-2.0 packages with a license file, Cognitive Forge authorship, and no links to non-public Cognitive Forge repositories.

## License

Apache-2.0. See [LICENSE](LICENSE). Packages keep their own license files and notices.
