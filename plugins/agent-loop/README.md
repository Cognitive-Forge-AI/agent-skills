# Agent Loop

Agent Loop adds reusable workflow skills for coding agents and chat assistants.

## Get started

Install the plugin from the Agent Skills marketplace.

Claude Code:

```sh
claude plugin marketplace add Cognitive-Forge-AI/agent-skills
claude plugin install agent-loop@agent-skills
```

Codex:

```sh
codex plugin marketplace add Cognitive-Forge-AI/agent-skills
codex plugin add agent-loop@agent-skills
```

GitHub Copilot CLI:

```sh
copilot plugin marketplace add Cognitive-Forge-AI/agent-skills
copilot plugin install agent-loop@agent-skills
```

Start a new session, then ask your agent for help, such as `Write a commit message for my staged changes.`

## Skills

| Skill | What it does | When it runs |
| --- | --- | --- |
| [commit-message](skills/commit-message/SKILL.md) | Writes a concise Conventional Commit-style message from a diff, staged changes, or a change summary. | When you ask for a commit message |

## Updates and other harnesses

The [marketplace README](https://github.com/Cognitive-Forge-AI/agent-skills#install) covers update commands, plus setup for VS Code, Cursor, OpenCode, and other harnesses that read skill directories.
