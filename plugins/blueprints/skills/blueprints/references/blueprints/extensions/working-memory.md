# Blueprints Working Memory Extension Specification

Status: draft
Version: 0.2.0-preview.1

## 1. Purpose

Working Memory keeps unresolved implementation context in Explorations. An Exploration is volatile: it gathers the possibilities, questions, assumptions, and observations around one concern, and it can be deleted once that context is recorded elsewhere or no longer useful. It may grow as the concern becomes clearer, and it may split when part of it is worth investigating on its own. [Project Memory](project-memory.md) owns the durable Specs and Decisions an Exploration links to.

## 2. Terminology

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://tools.ietf.org/html/rfc2119).

## 3. Definition

<!-- blueprints-module -->
```json
{
  "id": "working-memory",
  "version": "0.2.0-preview.1",
  "requires": [
    {"id": "project-memory", "mode": "active"}
  ]
}
```

<!-- blueprints-types:start -->
| Type | Durability | Statuses | Initial | Approved | Required fields |
| --- | --- | --- | --- | --- | --- |
| `exploration` | `volatile` | none | none | none | relationships |
<!-- blueprints-types:end -->

[Core §4.1](../core.md#41-durability-and-lifecycle) owns durability and lifecycle, plus shared metadata and adoption modes. Authoring an Exploration requires an active Project Memory selection.

## 4. Explorations

An Exploration's body SHOULD contain only context useful to investigating its concern, with empty sections omitted.

An Exploration MUST NOT establish desired intent or important project choices. Specs and Decisions own those meanings. Explorations SHOULD NOT be used primarily for task logs, generic question tracking, or investigation history; work history can remain in external sources.

An Exploration SHOULD record `updated` after its first meaningful edit and refresh it on later meaningful edits. Neither its presence nor its timestamps establish that its content is still valid.

## 5. Relationships

The table follows [Core §7](../core.md#7-core-relationships).

<!-- blueprints-relationships:start -->
| From | To | Label | Reciprocal | Level | Reason | Reciprocal rule | Use it when |
| --- | --- | --- | --- | --- | --- | --- | --- |
| exploration | spec | `explores` | none | SHOULD | OPTIONAL | none | The Exploration investigates possibilities relevant to realizing the Spec. |
| exploration | decision | `informs` | none | SHOULD | OPTIONAL | none | The Exploration contributes analysis to a draft or proposed Decision. |
<!-- blueprints-relationships:end -->

Every Exploration MUST record at least one `explores` link to a Spec or `informs` link to a Decision. A new `informs` link MUST target a Decision in `draft` or `proposed` status.

Neither label has a stored reciprocal: §4.1 forbids a durable item from storing one, and §6's incoming-link lookup finds the same connection without it.

## 6. Discovery

For implementation work, readers SHOULD find Explorations with incoming `explores` links to the relevant Spec. For work on a draft or proposed Decision, they SHOULD find those with incoming `informs` links to it. When work revisits a resolved Decision's choice, such as by proposing a replacement, readers SHOULD also find Explorations whose `informs` links were retained from its earlier states.

Within the consumer's discovery limits, scan summaries first and load only the bodies the task needs. Work on an entire Spec SHOULD load all direct Explorations when they fit; otherwise narrow the scope or report incomplete coverage. Discovery SHOULD NOT traverse Working Memory transitively by default or load it for unrelated Project Memory work.

Before relying on an Exploration, readers MUST check its assumptions against current Specs and Decisions.

## 7. Distillation and deletion

Before deleting an Exploration, authors MUST preserve intent and reasoning worth retaining in Specs and Decisions. Explorations MUST NOT be the sole record of reasoning behind an adopted choice.

An Exploration SHOULD be deleted once its context is recorded elsewhere or no longer useful, after repairing or removing incoming stored relationships. Discarded context needs no record. Distillation creates or updates the appropriate Spec or Decision; it MUST NOT change the Exploration's type or reuse its identity.

When a linked Spec becomes `active` or a linked Decision leaves `draft` or `proposed`, authors SHOULD review the Exploration: distill what is now settled, and delete it if nothing useful remains. The status change alone does not make an Exploration obsolete.

Core's reference-only retention rules still apply. Ordinary cleanup MUST NOT modify or delete retained reference-only items. Historical snapshot citations and their retention guarantees remain outside this extension; a commit hash alone does not guarantee content retention.

## 8. Acceptance

Check Core and binding rules, the required `explores` or `informs` link, relationship directions, and the absence of `status`. Change checks MUST distinguish new `informs` links from retained ones using a trusted baseline. A snapshot check MUST report that it cannot establish a link's creation state. Review content and distillation separately from structural checks.
