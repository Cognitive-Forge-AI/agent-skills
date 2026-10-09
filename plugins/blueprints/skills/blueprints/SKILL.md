---
name: blueprints
description: >
    Use when working in a repository that contains .blueprints/, when repository instructions require Blueprints, or when an authorized repository task changes intent, behavior, architecture, dependencies, security, operations, documentation meaning, or agent workflows.
disable-model-invocation: false
user-invocable: true
license: Apache-2.0
---

# Blueprints

Maintain small, linked project memory. Specs own current desired intent; Decisions own important reasoning. Apply the project's adopted capabilities and authority.

## Read relevant rules

1. Read [Core](references/blueprints/core.md) and the [Markdown binding](references/blueprints/markdown-based-implementation.md).
2. Read the project's `.blueprints/config.json`, whose `blueprints` version selects the suite for every extension. Load [Project Memory](references/blueprints/extensions/project-memory.md) when selected. Load [Working Memory](references/blueprints/extensions/working-memory.md) when selected and apply its Exploration workflow only when active. Load any other selected definition only from the available trusted bundle, and report unknown modules or versions rather than inventing rules or fetching executable code.
3. Apply [bounded discovery](references/discovery.md) before interpreting metadata or following targets. These references define the framework; the repository's `.blueprints/` contains its memory.

A missing configuration in an existing store is not permission to upgrade. Preserve its records, use available historical definitions for relevant reading, report interpretation gaps, and leave an upgrade for an explicit request. Do not silently stamp the installed version onto existing items.

For an authorized task requiring memory in a repository without a store, initialize `config.json` with the bundled suite version and `project-memory` active. Create a meaningful draft root Spec. Group additional items by feature only as needed.

## Choose the procedure per item

| Item state | Procedure |
| --- | --- |
| New or draft Spec; new, draft, or proposed Decision | [Drafting](references/drafting.md) |
| Exploration, when Working Memory is active | [Working Memory §§4–7](references/blueprints/extensions/working-memory.md#4-explorations) |
| Approved or historically resolved memory | [Maintenance](references/maintenance.md) |
| Item from a reference-only selection | Read relevant history; preserve its bytes and lifecycle |

A task may use more than one procedure. Follow active Specs and accepted Decisions for current authority, because a proposal does not replace them until it is approved.

Update a Spec when desired intent changes. Record a Decision for an important project choice. Refine wording without generating records that explain the edit. Run proportionate checks and report observations in the task or change review. A test run alone creates no Blueprint item.

Explorations carry no `status`. Find the ones whose `explores` or `informs` links name the Spec or Decision the task centers on, check their assumptions against current memory, and move lasting intent and reasoning into Specs and Decisions before deleting one. Never link from a Spec or Decision to an Exploration, whether as a new relationship or as a reciprocal.

## Retained history

Changing a selection to `reference-only` stops authoring without deleting or invalidating history. Existing identities and links stay resolvable. Active items may add valid links to retained items using the selected definitions, without claiming that old observations validate new behavior. If a mandatory reciprocal would require changing a retained item, report the constraint rather than editing the item or waiving the rule.

Load retained definitions and bodies only for a relevant connection or an explicit historical question. Missing definitions limit interpretation. They authorize neither deletion nor invented meaning, and ordinary work neither edits retained records nor activates their workflows.

## Validate and finish

Check identities, metadata, applicable type/version rules, relationship direction and reasons, targets, required reciprocals, and Project Memory ancestry when active. A reciprocal does not apply where durability forbids it. Telling a new `informs` link from a retained one needs a trusted baseline, so a snapshot check MUST report that it cannot establish when a link was created. Review content, authority, and code/intent drift separately, since structural checks cannot establish approval or behavioral correctness.

Preserve IDs, source provenance, and accepted reasoning. Report incomplete checks and unrelated defects accurately. Record no-impact conclusions with the change when none of the meaningful-change categories applies. A read-only explanation creates no memory artifacts unless requested.
