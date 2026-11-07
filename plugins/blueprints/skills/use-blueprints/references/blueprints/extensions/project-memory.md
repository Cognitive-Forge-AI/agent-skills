# Blueprints Project Memory Extension Specification

Status: draft
Version: 0.2.0-preview.1

## 1. Purpose

Project Memory captures current desired intent in Specs and durable reasoning in Decisions. It depends on Core and defines the first complete Blueprints authoring capability, without requiring any additional execution record.

## 2. Terminology

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://tools.ietf.org/html/rfc2119).

## 3. Definition

<!-- blueprints-module -->
```json
{
  "id": "project-memory",
  "version": "0.2.0-preview.1",
  "requires": []
}
```

<!-- blueprints-types:start -->
| Type       | Durability | Statuses                                                               | Initial             | Approved   | Required fields |
|------------|------------|------------------------------------------------------------------------|---------------------|------------|-----------------|
| `spec`     | `durable`  | `draft`, `active`, `removed`                                           | `draft`             | `active`   | none            |
| `decision` | `durable`  | `draft`, `proposed`, `accepted`, `superseded`, `rejected`, `withdrawn` | `draft`, `proposed` | `accepted` | none            |
<!-- blueprints-types:end -->

The declaration owns these statuses. New items MUST use a permitted initial status, and creation MUST NOT include advancement to an approved status. Approval is explicit and governed by project authority: a human, review process, CI system, or agent with a stated grant. An upgrade preserves established equivalent lifecycle meaning. It creates no new item and erases no existing approval.

## 4. Meaningful memory

A Spec MUST be created or updated when desired intent or behavior changes. A Decision MUST record an important project choice: architecture, dependencies, security, persistence, deployment, or agent workflow. Editing text alone is not an important project choice.

An item MUST carry information that changes how a reader acts. Its body SHOULD remain at most about 400 words, because an item that outgrows that budget usually owns more than one meaning. Split the larger meanings using Core decomposition relationships. Omit empty sections and information already owned elsewhere. Authors MUST NOT invent constraints, alternatives, consequences, or trade-offs to complete a template.

## 5. Specs

A Spec owns current desired intent and behavior. It MUST describe the desired state in the present tense, remain at the same stable ID as intent evolves, and use `removed` when that intent is withdrawn.

For example: “The API rejects requests without a valid token.” A change narrative such as “We will replace cookies with tokens” belongs outside the Spec's current-state body.

A Spec MAY state real constraints, non-goals, and artifact anchors. It SHOULD link to the reasoning behind its intent. It MUST NOT become a history log or repeat generated code documentation.

## 6. Decisions

A Decision owns one important decision problem or choice and its durable reasoning. A new Decision MUST start at `draft` for an unresolved choice or `proposed` for a concrete proposal. A draft MUST state the problem and relevant context; it needs no Y-statement. A proposed Decision MUST state the choice as a Y-statement:

> In the context of `<use case>`, facing `<concern>`, we decided for `<option>` to achieve `<quality>`, accepting `<downside>`.

The long form MAY include genuinely evaluated alternatives and additional rationale. Omit unsupported details, and ask about missing material reasoning rather than inventing a downside. The statement in a `proposed` item expresses a proposal, not approval.

Drafting refines a draft or proposed Decision in place. Accepted, rejected, and superseded Decisions MUST retain the proposal and its reasoning.

A draft with nothing worth retaining MAY be deleted after repairing or removing incoming stored relationships. A Decision still in `draft` or `proposed` MAY become `withdrawn`; it MUST retain its question or proposal and explain why work stopped. `rejected` records an evaluated proposal's rejection. Accepted, rejected, and superseded Decisions MUST NOT be deleted or withdrawn through this cleanup procedure.

A substantive change to an accepted choice MUST create a proposed replacement that preserves the earlier record. The earlier Decision remains accepted until the replacement is accepted. At that approval, record both supersession relationships and change the earlier status to `superseded`. Clarifying wording without changing the choice does not require a replacement.

## 7. Relationships

The table follows the reading rules in [Core §7](../core.md#7-core-relationships).

<!-- blueprints-relationships:start -->
| From     | To       | Label        | Reciprocal      | Level  | Reason   | Reciprocal rule | Use it when                                                             |
|----------|----------|--------------|-----------------|--------|----------|-----------------|-------------------------------------------------------------------------|
| spec     | spec     | `related-to` | `related-to`    | SHOULD | REQUIRED | SHOULD          | Either Spec supplies useful direct context for understanding the other. |
| spec     | spec     | `depends-on` | `required-by`   | SHOULD | REQUIRED | SHOULD          | The source requires a capability or contract defined by the target.     |
| decision | spec     | `affects`    | `affected-by`   | SHOULD | OPTIONAL | SHOULD          | The choice changes what the Spec means or requires.                     |
| decision | decision | `supersedes` | `superseded-by` | MUST   | OPTIONAL | MUST            | An accepted replacement replaces an earlier accepted choice.            |
<!-- blueprints-relationships:end -->

`related-to` is symmetric and non-transitive, and its reason MUST identify the useful context. `depends-on` is directional. Either direction's reason MUST identify the required capability or contract and why it is needed. Prefer the dependency pair when that more specific relationship applies. Review incoming and outgoing connections when intent changes.

## 8. Drafting and maintenance

Choose the procedure per affected item, not once for the whole task.

- For `draft` Specs and `draft` or `proposed` Decisions, revise the owned meaning in place. Keep only current wording and actual reasoning. Iterations do not generate a change log or Decisions justifying edits.
- For approved memory, preserve authority and reasoning. Update Specs in place under the project's review process. Supersede accepted choices only through §6. Do not treat a request to explore alternatives as approval to replace current intent.
- A task can refine a draft while maintaining an approved item. Apply the appropriate rule to each.

Code owns implemented state; Specs own desired state; Decisions own reasoning; external sources own original requests. Where code and its Spec disagree, that is drift to report rather than permission to overwrite intent. Supersession SHOULD trigger review of affected Specs for stale reasoning.

A change has no Blueprint impact only when it changes none of intent, behavior, architecture, dependency posture, security, operations, documentation meaning, or agent workflow. Record no-impact conclusions with the change, without creating a ceremonial item.

## 9. Markdown integration

These rules apply when Project Memory is active. The generic Markdown binding does not impose them on other extensions.

The active store MUST have at least one root Spec describing project intent. Feature folders group meaningful areas, not item types. A folder-level Spec SHOULD exist only when the area owns shared intent, and it MUST NOT merely list its children.

A folder-level Spec is identified by its owned area and its decomposition links rather than by an ID mirroring today's path. For a new hierarchy, its ID SHOULD initially reflect its feature path. Subsequent folder moves MUST preserve IDs and filenames.

Every non-root Spec MUST record `part-of` to the nearest owning folder-level Spec: another owning Spec in the same folder for a local child, otherwise the nearest ancestor folder-level Spec, falling back to a root Spec. A folder-level Spec itself uses an ancestor, never itself. Where several plausible owners exist, resolve ownership from intent and explicit relationships rather than picking by filename. Root Specs record no ancestry `part-of`.

Discovery starts in the task's feature: read its owning Spec, then relevant child Specs and related Decisions. In a grouping folder, inspect contained Specs and the nearest folder-level Specs in descendant branches. Use summaries and relationship reasons before loading bodies, and broaden only when local context is insufficient.

## 10. Acceptance

Check applicable Core and Markdown rules, statuses, Spec ancestry, relationship reasons, and supersession reciprocity. Review content and approval independently, under the validation limits in [Core §8](../core.md#8-validation). With this extension reference-only, its historical definitions remain usable, and no authoring workflow or root-Spec creation is activated.
