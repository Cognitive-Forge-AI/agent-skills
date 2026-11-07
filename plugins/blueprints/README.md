# Blueprints

Blueprints gives you and your AI agents a shared project memory, stored in your repository as small, linked Markdown files. Specs capture current desired intent. Decisions preserve the reasoning behind important choices. Agents get focused context without turning the memory into a task log, and you review every change in Git.

## Get started

Install the plugin from the Agent Skills marketplace.

Claude Code:

```sh
claude plugin marketplace add Cognitive-Forge-AI/agent-skills
claude plugin install blueprints@agent-skills
```

Codex:

```sh
codex plugin marketplace add Cognitive-Forge-AI/agent-skills
codex plugin add blueprints@agent-skills
```

GitHub Copilot CLI:

```sh
copilot plugin marketplace add Cognitive-Forge-AI/agent-skills
copilot plugin install blueprints@agent-skills
```

Start a new session in your repository, then send one of these prompts. The agent asks its questions first, so you can answer them and leave it to work.

| Goal | Claude Code | Any agent |
| --- | --- | --- |
| Choose extensions when asked | `/blueprints:initialize-blueprints` | `Set up Blueprints in this repository.` |
| Enable a specific extension | `/blueprints:initialize-blueprints with Working Memory` | `Set up Blueprints in this repository with Working Memory.` |

The agent confirms the project name, asks which extensions to enable, and asks whether to add a line to `AGENTS.md` or `CLAUDE.md` that reminds future sessions to use Blueprints. It then creates `.blueprints/config.json` and a draft root Spec describing your project. Review the Spec, edit anything it got wrong, and commit the files.

To bring in existing specs, ADRs, or design notes, run `migrate-to-blueprints`.

## Skills

| Skill | What it does | When it runs |
| --- | --- | --- |
| [initialize-blueprints](skills/initialize-blueprints/SKILL.md) | Creates a new store in a repository that has none. | When you ask to set up Blueprints |
| [use-blueprints](skills/use-blueprints/SKILL.md) | Keeps Specs, Decisions, and optional Explorations current as the project changes. | During repository work in a project that uses Blueprints |
| [migrate-to-blueprints](skills/migrate-to-blueprints/SKILL.md) | Imports intent and reasoning from existing project documents and leaves the originals intact. | When you run it by name |
| [upgrade-blueprints](skills/upgrade-blueprints/SKILL.md) | Moves an existing store to a newer framework version, preserving IDs, provenance, and valid approvals. | When you run it by name |

Run the last two explicitly:

| Harness | Command |
| --- | --- |
| Claude Code | `/blueprints:<skill>` |
| Codex | `$<skill>` |
| GitHub Copilot CLI | `/<skill>` |

## How it works

Your choice of extensions lives in `.blueprints/config.json`, and each one adds its own kind of memory:

| Item | Holds | Extension |
| --- | --- | --- |
| Spec | Current desired behavior, updated in place as intent evolves | Project Memory |
| Decision | An important choice and the reasoning behind it | Project Memory |
| Exploration | Unresolved implementation context, deleted once it stops being useful | Working Memory (optional) |

New Specs and Decisions start as drafts or proposals, and approving one is a separate, explicit step. An accepted Decision stays in place until a replacement is approved.

A few habits keep the memory useful:

- Describe what the project should do in Specs, keep important reasoning in Decisions, and link the two.
- Move anything lasting out of an Exploration before deleting it.
- Keep task progress and check output in your normal task and review records.

The packaged definitions cover the details: [Core](skills/use-blueprints/references/blueprints/core.md), the [Markdown binding](skills/use-blueprints/references/blueprints/markdown-based-implementation.md), [Project Memory](skills/use-blueprints/references/blueprints/extensions/project-memory.md), and [Working Memory](skills/use-blueprints/references/blueprints/extensions/working-memory.md).

## Updates and other harnesses

Updating the plugin doesn't change your store. Your project keeps its declared version until you run `upgrade-blueprints`.

The [marketplace README](https://github.com/Cognitive-Forge-AI/agent-skills#install) covers update commands, plus setup for VS Code, Cursor, OpenCode, and other harnesses that read skill directories.
