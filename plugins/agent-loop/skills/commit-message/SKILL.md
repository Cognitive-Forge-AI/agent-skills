---
name: commit-message
description: Generate concise Conventional Commit-style commit messages from a supplied diff, staged changes, working-tree changes, or an equivalent change summary. Use when asked to write, suggest, or refine a Git commit message, including in ChatGPT-style chats and coding agents such as Claude Code or Codex. Respect applicable project/chat/repository instructions and explicit scope vocabularies; use an existing scope only when it is clearly defined and applicable, and never invent a scope when none is defined.
license: Apache-2.0
---

# Commit Message

Generate a short, human-readable commit message that describes the purpose or
impact of the change rather than listing files or implementation details.

## Gather context

1. Analyze the diff or change summary supplied by the user.
2. In a coding environment, if the user refers to current or staged changes
   without pasting a diff, inspect the relevant Git diff available to the agent.
3. Apply all applicable instructions already defined by the host environment,
   chat/project, or repository. Examples include project instructions,
   `AGENTS.md`, contribution guidelines, and release configuration.
4. Use project-defined commit types, scopes, or formatting rules when they
   exist and do not conflict with higher-priority instructions.
5. Do not invent project-specific conventions that are not defined.

## Resolve the scope

Prefer a scope only when the project or conversation explicitly defines a
scope vocabulary and the change maps clearly to one of those scopes.

Examples of valid scope sources include:

- A scope list or commit convention in chat/project instructions.
- `AGENTS.md`, contribution documentation, or equivalent repository guidance.
- A release configuration such as `release-please-config.json` whose
  `component` values are used by the project as commit scopes.
- Another explicit repository-maintained scope mapping.

When a release configuration defines component scopes, use the exact component
name that applies to the changed files or component. Do not create a new scope
from a directory, package, service, feature name, or your own interpretation.

If multiple defined scopes apply and the project does not define a combined
scope, omit the scope rather than inventing one.

If no scope vocabulary is defined, omit the scope. An unscoped commit is better
than a guessed scope.

## Choose the type

Use the most accurate type. Do not default to `feat` merely because code was
added or changed.

- `fix`: Correct a bug or unintended behavior.
- `build`: Change build systems, packaging, or external dependencies.
- `chore`: Perform routine maintenance that does not change product behavior.
- `ci`: Change CI/CD configuration or automation.
- `docs`: Change documentation only.
- `perf`: Improve performance without changing intended behavior.
- `refactor`: Restructure code without fixing a bug or adding behavior.
- `revert`: Revert a previous commit.
- `style`: Change formatting or other non-behavioral code style.
- `test`: Add, remove, or correct tests without changing product behavior.
- `feat`: Introduce new product, user, API, or business capability.

If the project explicitly defines additional types, use them when applicable.

## Write the subject

Use this structure:

`<type>(<scope>): <subject>`

Omit `(<scope>)` when no valid scope is available.

Requirements:

- Use imperative mood: `add`, `fix`, `remove`, `prevent`, `allow`.
- Describe purpose or impact, not files or implementation mechanics.
- Keep it human-readable and specific.
- Keep the full subject line at 72 characters maximum.
- Aim for about 50 characters when practical.
- Prefer a single concise line over extra detail.

Good:

`fix(auth): prevent expired sessions from being reused`

`docs: clarify local development prerequisites`

`refactor(api): simplify request validation flow`

Avoid:

`feat(api): update UserController.cs and helper files`

`chore(core): miscellaneous changes`

## Add a body only when useful

Separate the subject from the body with a blank line.

Use a body only when the reason, context, or consequence cannot be understood
well enough from the subject alone.

The body must:

- Explain **why** the change was needed.
- Give enough context for someone unfamiliar with the change.
- Avoid explaining **how** it was implemented.
- Avoid low-level technical details already visible in the diff.
- Wrap lines at about 72 characters.
- Stay as short as possible.

Example:

`feat(profile): allow users to upload profile pictures`

`Support account personalization during signup and profile editing.`

## Add footers only from explicit context

Add issue references only when the user provides them or they are explicitly
available in the task context.

Do not invent issue numbers, ticket IDs, links, or references.

Add a breaking-change marker or `BREAKING CHANGE:` footer only when the user
explicitly asks for breaking-change notation or applicable project instructions
require it from information already provided. Do not infer or invent release
notes, version bumps, or migration instructions.

## Handle ambiguity

Do not invent business intent or user impact that cannot be supported by the
diff and available project context.

If ambiguity would materially change the commit type or subject, ask one
precise clarification question when interaction is possible. Otherwise, use
the narrowest defensible wording supported by the changes.

## Output

Once enough context is available to generate the commit message, output only
the commit message itself.

Do not add markdown fences, headings, explanations, alternatives, analysis, or
introductory text.
