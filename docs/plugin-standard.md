# Agent Skills plugin standard

This standard defines how a plugin is packaged for the Agent Skills marketplace, and how a Cognitive Forge source repository (a *spoke*) declares it. Authoring skills, maintainers, and contributors can follow it without other instructions. `agent-skills validate` checks it, together with [`catalog-policy.json`](../catalog-policy.json).

The key words MUST, MUST NOT, SHOULD, and MAY are used as in RFC 2119.

## 1. Package layout

A plugin is one directory, `plugins/<plugin-name>/` in the marketplace. Harnesses install this directory as the plugin, so everything the plugin needs MUST be inside it:

```text
plugins/<plugin-name>/
├── .claude-plugin/plugin.json   # Claude Code; also read by Copilot and VS Code
├── .codex-plugin/plugin.json    # Codex
├── .cursor-plugin/plugin.json   # Cursor
├── LICENSE                      # Apache-2.0 text
├── NOTICE                       # Copyright and attribution
├── SECURITY.md                  # Security policy
├── README.md                    # For people installing the plugin
├── CHANGELOG.md                 # Optional release notes
├── skills/<skill-name>/SKILL.md # Optional components below
├── agents/
├── commands/
├── hooks/hooks.json
└── .mcp.json
```

- The plugin name MUST be kebab-case (`^[a-z0-9]+(-[a-z0-9]+)*$`) and unique in the marketplace.
- A plugin MUST declare at least one component: skills, agents, commands, hooks, MCP servers, or apps.
- Every path a manifest references MUST exist inside the plugin directory. Links (symbolic links, junctions) are not allowed anywhere in the path.
- Every manifest's `name` and `version` MUST match the plugin's catalog entry.

## 2. Skills

Each skill is a directory containing `SKILL.md`, with YAML frontmatter:

```markdown
---
name: <skill-name>
description: <what it does and when to use it, in one paragraph>
license: Apache-2.0
---
```

- `name` MUST match the directory name and be kebab-case.
- `description` MUST say what the skill does and when an agent should use it.
- `license` SHOULD be present. A skill copied on its own (OpenCode, the Vercel `skills` CLI, manual copies) keeps its license this way.
- Supporting files (`references/`, `scripts/`, `assets/`) live inside the skill directory and are referenced relatively.

## 3. Licensing and notices

- Plugins are licensed under Apache-2.0. Every catalog entry and plugin manifest MUST declare `"license": "Apache-2.0"`.
- Every plugin MUST contain `LICENSE`, `NOTICE`, `README.md`, and `SECURITY.md` at its root. `NOTICE` names the plugin and states `Copyright <year> Cognitive Forge Inc.`; Apache-2.0 §4(d) requires redistributors to keep it.
- Third-party material MUST keep its own license and attribution, and be identified in `NOTICE`.

## 4. Metadata

- `author` is `Cognitive Forge` (`opensource@cognitiveforge.ai`); plugins are company works.
- `homepage` points at `https://github.com/Cognitive-Forge-AI/agent-skills/tree/main/plugins/<plugin-name>` and `repository` at the marketplace.
- Content MUST NOT link to non-public Cognitive Forge repositories, private issues, or internal project memory.

## 5. Versions

- Versions follow SemVer. Previews use a prerelease suffix, such as `0.2.0-preview.1`.
- A published version never changes content. Any change ships under a new version.
- The marketplace version (`marketplace.json`) increases by one minor version per release that changes plugins.

## 6. Spoke repositories

A Cognitive Forge spoke publishes exactly one plugin through the private registry. Its `ai-capabilities/manifest.json` follows [`schemas/spoke-manifest.schema.json`](../schemas/spoke-manifest.schema.json):

```json
{
  "$schema": "https://raw.githubusercontent.com/Cognitive-Forge-AI/agent-skills/main/schemas/spoke-manifest.schema.json",
  "schemaVersion": 2,
  "name": "<plugin-name>",
  "plugins": [
    {
      "name": "<plugin-name>",
      "displayName": "<Display Name>",
      "description": "<One-sentence description>",
      "version": "0.1.0-preview.1",
      "category": "productivity",
      "skillsPath": "skills",
      "assets": [
        { "source": "plugin-readme.md", "destination": "README.md" },
        { "source": "CHANGELOG.md" }
      ]
    }
  ]
}
```

- `schemaVersion` is required and MUST be `2`.
- The registry generates the three plugin manifests and applies Cognitive Forge authorship and the Apache-2.0 license. Spokes do not declare `author`, `owner`, or `license`.
- Component paths are relative to the capability root and copied with their relative paths.
- The spoke repository root also carries `LICENSE` and `NOTICE` for the repository itself.
- Shared workflows from the registry notify it of changes and validate pull requests against this standard.

### 6.1 Package assets

`assets` declares extra files for the package. `source` names a file or directory relative to the capability root. `destination` is a path relative to the package root, and defaults to `source`. The schema checks the shape of each entry. The registry decides which destinations it accepts when it compiles a plugin, and reports any it rejects. The resulting package must still meet this standard.

### 6.2 Plugin README

The plugin README is an `assets` entry with the destination `README.md`. Its source file can have another name:

```json
{ "source": "plugin-readme.md", "destination": "README.md" }
```

The README is written for people installing and using the plugin: what it does, which harnesses it supports, how to install it, and how to use its components. Guidance for authors and maintainers belongs in the spoke repository, not here. A capability root's own `README.md` usually describes the source, so the plugin README is often a separate file, such as `plugin-readme.md`.

### 6.3 Changelog

A plugin MAY publish `CHANGELOG.md` at the package root by declaring it in `assets`.

- Entries are written for people using the plugin.
- Entries MUST NOT contain commit SHAs or private references.
- An entry MUST ship in the same change as its version bump.

### 6.4 Markdown links

These rules apply to every Markdown file in the package. The registry enforces them at release. `agent-skills validate` only checks the URL prefixes in [`catalog-policy.json`](../catalog-policy.json).

- A file MUST NOT reference a non-public Cognitive Forge repository, in any form and whether or not the reference is a link. That file lists the forbidden URL prefixes and the allowed exceptions. The rule also covers forms it does not list: raw file URLs, GitHub Pages URLs, SSH remotes, and `Cognitive-Forge-AI/<repo>` shorthand.
- Relative links MUST resolve to a file inside the published package. They are resolved from the linking file's folder in the package, not from the source file's location in the spoke. A link MAY use `../` if it still ends inside the package.
- Every other link MUST use the `https` or `mailto` scheme.
