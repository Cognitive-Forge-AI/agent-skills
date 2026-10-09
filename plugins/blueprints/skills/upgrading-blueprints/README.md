# Upgrading Blueprint stores

This skill moves an existing `.blueprints/` store to a newer Blueprints framework version, preserving stable IDs, provenance, valid approvals, and supported content. The default target is 0.2.0-preview.1.

**It runs only when you ask for it.** `agents/openai.yaml` sets `allow_implicit_invocation: false`, and the skill's own first instruction is to confirm the request before scanning. An agent that notices a version mismatch during other work is meant to mention it and carry on, not to start rewriting project memory.

[SKILL.md](SKILL.md) is the procedure. It routes each supported source version to its catalog under `references/`, and names the scripts that belong to the preview.1 → preview.4 step.
