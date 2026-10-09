---
name: migrating-to-blueprints
description: >
    Use when the user asks to convert existing project documents, specifications, ADRs, design notes, issues, or historical material into Blueprints project memory.
disable-model-invocation: true
user-invocable: true
context: fork
license: Apache-2.0
---

# Migrating to Blueprints

Import durable intent and reasoning from other documentation. For an existing Blueprint store changing framework versions, use `upgrading-blueprints` instead.

## Inspect and classify

Read [Core](../blueprints/references/blueprints/core.md), the [Markdown binding](../blueprints/references/blueprints/markdown-based-implementation.md), and [Project Memory](../blueprints/references/blueprints/extensions/project-memory.md), plus [Working Memory](../blueprints/references/blueprints/extensions/working-memory.md) when the project selects it. Use the everyday skill's [bounded discovery](../blueprints/references/discovery.md) and trust boundary.

Inventory candidate documents with targeted file searches. Read project entry points, relevant sources, and change history where authority is unclear. Preserve originals. Record a short working inventory of source, owned meaning, authority, target feature, and unresolved questions outside the Blueprint item bodies.

| Source meaning | Destination |
| --- | --- |
| Current desired behavior, constraints, invariants | Spec |
| Important unresolved choice and durable context | Draft Decision |
| Concrete proposal, rationale, evaluated alternatives | Proposed Decision |
| Unresolved context for one open concern: possibilities, questions, assumptions | Exploration, only when Working Memory is active |
| Execution history, observations, checklists, review output | Preserve as source material; extract intent or reasoning only if actually present |
| Generated API documentation or code models | Reference the source or artifact rather than copying it |

Resolve ambiguity when it materially changes accepted intent or ownership. Do not infer a requirement solely from current implementation. Historical assertions are not automatically current desired state.

## Import only meaningful memory

If no store exists, create the standard project declaration with Project Memory active and a draft root Spec describing actual project intent. Select `working-memory` active only when the project needs Explorations; it requires active Project Memory. For an existing store, use its configuration rather than inferring an upgrade or reactivating reference-only definitions.

Reuse existing items for the same meaning. Group by feature, maintain stable IDs, and establish the Project Memory ancestry links. Create folder-level Specs only for real shared intent. Link original sources and preserve provenance.

Write Specs in present tense. Write draft Decisions around their unresolved problem; use grounded Y-statements for concrete proposals. Include only supported reasoning and genuinely evaluated alternatives. Omit empty sections, and split an item that outgrows the ~400-word body budget.

Newly imported Specs and Decisions start `draft` or `proposed`. Do not infer approval from a source's title or invent missing trade-offs. Existing Blueprint items retain their identity and approval history, which importing a source does not reset. Explicit approval happens separately under project authority.

## Verify and report

Check metadata, IDs, type/version rules, durability and lifecycle requirements, targets, required reasons and reciprocals, root and ownership ancestry. Review that source meaning was preserved and no current intent was silently inferred from history. Report unresolved interpretation in the task's working inventory.

Leave originals intact. Archiving or deletion is optional, requires an explicit scope, and must account for inbound references. Check results and migration progress belong in ordinary task/change-review output.
