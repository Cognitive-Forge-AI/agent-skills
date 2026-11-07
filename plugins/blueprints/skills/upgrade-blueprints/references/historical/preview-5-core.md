# Blueprints Core Specification

Status: draft
Version: 0.1.0-preview.5

## 1. Purpose

Blueprints is a strict-by-default, minimal-by-design methodology for capturing durable project memory.

Blueprints defines how intent, reasoning, execution, and validation relate to one another so humans and AI agents can work with shared context.

This core specification defines the memory model and relationships of Blueprints.

## 2. Terminology

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://tools.ietf.org/html/rfc2119).

## 3. Type system

Blueprints defines these core field types:

- `String`: Unicode text.
- `Identifier`: a stable string identifier for a blueprint item, constrained by the grammar in Section 5.1.
- `URI`: a URI reference as defined by [RFC 3986](https://www.rfc-editor.org/rfc/rfc3986), allowing either a relative reference or an absolute URI.
- `Timestamp`: an ISO 8601 date or date-time.
- `Enum`: a string constrained to a defined set of values.
- `Array<T>`: an ordered list of values of type `T`.
- `Object`: a structured value with named fields.
- `Source`: an external reference composed of a source type, URI reference, and optional description.
- `Relationship`: a directed relationship to another blueprint item, external source, or project anchor.

### 3.1 Constraints

- `required`: the field MUST be present.
- `non-empty`: the value MUST contain at least one non-whitespace character.
- `unique`: the value MUST be unique within the blueprint system.
- `stable`: the value SHOULD NOT change after creation.
- `enum`: the value MUST be one of the allowed values.
- `short`: the value SHOULD fit on one line.
- `lower-kebab-case`: the value MUST use lowercase letters, numbers, and hyphens.
- `resolvable`: the value SHOULD be usable by humans, agents, or tools to locate the referenced target.
- `immutable`: the value SHOULD identify a specific point in time or revision.
- `semver`: the value MUST conform to Semantic Versioning 2.0.0 (major.minor.patch, with optional pre-release identifier and build metadata), as defined at [semver.org](https://semver.org/).

### 3.2 Field definition format

Fields are defined using this compact format:

- `name`: `Type` [`constraints`] - Meaning.

Example:

- `id`: `Identifier` [`required`, `unique`, `stable`, `non-empty`] - Stable blueprint item identifier.

## 4. Core model

A Blueprint system is composed of **blueprint items**.

A blueprint item is a durable unit of project memory. Each item captures one kind of meaning and relates to other items.

The framework defines these core item types:

- **Spec**: captures current desired intent.
- **Decision**: captures reasoning, trade-offs, and consequences.
- **Plan**: captures intended execution, progress, validation steps, and handoff state.
- **Evidence**: captures validation results.

## 5. Metadata model

Every blueprint item MUST have metadata.

Metadata is used for identification, lifecycle state, routing, relationships, and validation.

Every blueprint item MUST include:

- `id`: `Identifier` [`required`, `unique`, `stable`, `non-empty`] - Stable blueprint item identifier.
- `type`: `ItemType` [`required`, `enum`] - Blueprint item type. Values: `spec`, `decision`, `plan`, `evidence`.
- `status`: `Enum` [`required`, `enum`] - Lifecycle state using the status values defined by the item's type.
- `summary`: `String` [`required`, `non-empty`, `short`] - Relevance preview for humans and agents.
- `created`: `Timestamp` [`required`] - Creation date or timestamp.
- `blueprints`: `String` [`required`, `non-empty`, `semver`] - Blueprints framework version the item was authored under.

Blueprint items SHOULD include:

- `sources`: `Array<Source>` - External references that contributed to the item.
- `relationships`: `Array<Relationship>` - Directed relationships to blueprint items, external sources, or project anchors.
- `updated`: `Timestamp` - Last meaningful update date or timestamp.

Blueprint item metadata MUST be machine-readable.

Blueprint item metadata SHOULD be small enough for agents to scan first and decide whether the item is relevant enough to load in full.

### 5.1 Identifier rules

An item `id` MUST be stable across renames, moves, and storage changes.

An item `id` MUST be unique within the blueprint system.

An item `id` SHOULD be human-readable.

An item `id` MUST match this grammar:

```abnf
id       = segment *( "." segment )
segment  = alphanum *( [ "-" ] alphanum )
alphanum = %x61-7A / %x30-39   ; lowercase ASCII letter or digit
```

A segment MUST NOT be empty, and MUST NOT begin or end with a hyphen.

The canonical URI for a blueprint item MUST be `blueprint:<id>`, where `<id>` is the item's exact stable `id`. Implementations MUST resolve this URI by item ID within the blueprint system rather than by a storage-relative path, so the reference remains valid when the item is renamed, moved, or stored elsewhere.

Every character the identifier grammar allows is an RFC 3986 unreserved character. The canonical URI is therefore formed by concatenating `blueprint:` with the `id` verbatim, and resolved by removing that prefix and using the remainder verbatim. An implementation MUST NOT percent-encode, percent-decode, case-fold, or otherwise normalize the `<id>` portion.

A validator MUST reject an `id` that does not match the identifier grammar, and MUST perform that check before resolving relationships, so a malformed target is reported as an invalid identifier rather than as a missing item.

### 5.2 Source references

Blueprint items SHOULD include `sources` when intent, reasoning, or work originates from an external reference.

A `Source` is an external reference that contributed to a blueprint item.

Source fields:

- `type`: `SourceType` [`required`, `enum`, `lower-kebab-case`] - Source classification. Values: `github-issue`, `github-pull-request`, `document`, `web-page`, `ticket`, `other`.
- `uri`: `URI` [`required`, `resolvable`] - URI reference that identifies the source.
- `description`: `String` [`non-empty`] - Meaning of the source type when `type` is `other`, or additional source context when useful.

When `type` is `other`, `description` MUST be present.

### 5.3 Relationships

Blueprint item relationships SHOULD be represented using `relationships`.

Relationship fields:

- `label`: `String` [`required`, `non-empty`, `lower-kebab-case`] - Relationship meaning.
- `target`: `URI` [`required`, `resolvable`] - URI reference for a related blueprint item, external source, or project anchor. A blueprint-item target MUST use the canonical `blueprint:<id>` URI defined in Section 5.1.
- `reason`: `String` [`non-empty`, `short`] - Purpose of the relationship for the source item. Presence requirements are defined in [Section 11.1](#111-relationship-reference).
- `revision`: `String` [`immutable`] - Revision that identifies the target at a specific point in time. It is REQUIRED when `label` is `anchored-to` and OPTIONAL for all other relationship labels.

An `anchored-to` target MUST be a project-artifact URI. The project-artifact URI has one form: `file:<path>`, where `<path>` is a non-empty, repository-relative path. The URI MUST NOT have an authority, query, or fragment, and its decoded, normalized path MUST remain within the repository root. An `anchored-to` target MUST NOT use `blueprint:` or any URI form other than this repository-relative `file:` form.

An `anchored-to` revision MUST use one of these immutable forms:

- `git:<40-lowercase-hex-digits>` for a full SHA-1 Git commit object ID.
- `git:<64-lowercase-hex-digits>` for a full SHA-256 Git commit object ID.

Abbreviated object IDs, symbolic references, branch names, and tag names are not immutable revision forms.

To validate an `anchored-to` relationship, a validator MUST resolve the repository root that owns the Blueprint system, decode and normalize the `file:` path, reject an absolute path or any path that escapes that root, resolve the complete object ID without prefix matching, verify that the object is a Git commit, and verify that the normalized path exists in that commit's tree. The validator MUST NOT substitute the working-tree version of the target or another revision. The relationship is valid only when both the commit and the path at that commit resolve.

The `revision` field remains OPTIONAL for every relationship label other than `anchored-to`; those labels do not use the `anchored-to` revision syntax or resolution procedure unless another specification defines one.

Relationships are the primary way to compose context.

Blueprint items SHOULD NOT duplicate content from related items unless the duplication is necessary for clarity.

### 5.4 Lifecycle and approval

Each item type defines its own `status` values in Sections 7 to 10. The first value listed for a type is its initial status.

A newly created blueprint item MUST use its type's initial status: `draft` for a Spec, `proposed` for a Decision, `draft` for a Plan, and `recorded` for Evidence.

An author MUST NOT create an item at an approved status, and MUST NOT advance an item in the same operation that creates it.

Advancing a Spec to `active`, a Decision to `accepted`, or a Plan to `ready` MUST be an explicit approval. A project defines who or what approves: a human, a review process, a CI system, or an agent acting under a stated grant of authority.

## 6. Content model

Every blueprint item MUST have content.

Content captures the meaning owned by the item.

Each item SHOULD own one kind of meaning:

- Specs own current desired state.
- Decisions own reasoning.
- Plans own execution state.
- Evidence owns validation results.

Blueprint item content SHOULD be minimal and focused.

### 6.1 Meaningful content

A blueprint item MUST exist only when it carries meaning that a reader cannot get from the code, from a related item, or from the change history.

Every section of a blueprint item MUST carry information that changes how a reader acts. A section with no real content MUST be omitted rather than filled.

An author MUST NOT invent constraints, alternatives, consequences, non-goals, or validation results to complete a section or to make an item look complete.

Blueprint item content MUST NOT restate content owned by a related item, and MUST NOT become a general-purpose note dump.

A blueprint item body SHOULD be at most 400 words. An item that outgrows that budget SHOULD be split into child items, related as defined in Section 11.1.

### 6.2 Meaningful items

Specs are the backbone of project memory. Decisions, Plans, and Evidence support Specs and MUST be created only when they are meaningful on their own.

An item is meaningful when it carries the kind of meaning its type owns:

- A Spec is meaningful when intent or desired behavior changes. A Spec MUST be created or updated in that case.
- A Decision is meaningful when the work makes an important choice, as defined in Section 8. A Decision MUST be created in that case, and the Specs it affects usually relate to it.
- A Plan is meaningful when execution spans multiple sessions or agents, or needs handoff. A Plan MUST be created in that case, is OPTIONAL otherwise, and MUST NOT be created solely to satisfy this framework.
- Evidence is meaningful when a validation step was executed and its result matters, as defined in Section 10. Evidence MUST be created in that case, is OPTIONAL otherwise, and MUST NOT be created for work that was authored but not validated.

## 7. Specs

Spec-specific metadata:

- `status`: `SpecStatus` [`required`, `enum`] - Spec lifecycle state. Values: `draft`, `active`, `removed`.

A Spec captures the current desired intent.

A Spec MUST be updated in place as its intent evolves and use `removed` when that intent is withdrawn.

A Spec MUST describe the system in the present tense, as the state expected to be true now.

A Spec MUST state its intent and current desired behavior.

A Spec MAY also state constraints, non-goals, anchors to project artifacts, and relationships to related items, when those carry real content as defined in Section 6.1.

A Spec MUST NOT be written as planned work, as a change narrative, or as a history log.

- Spec: The API rejects a request without a valid token.
- Not a Spec: We will add token validation, replacing the old cookie check.

A Spec SHOULD NOT duplicate code models, implementation details, or generated documentation.

A Spec SHOULD relate to the reasoning behind its intent and to the project artifacts that realize it, using the relationships defined in Section 11.1.

## 8. Decisions

Decision-specific metadata:

- `status`: `DecisionStatus` [`required`, `enum`] - Decision lifecycle state. Values: `proposed`, `accepted`, `superseded`, `rejected`.

A Decision is an architecture decision record. It captures one important choice, its reasoning, and its consequences.

A Decision MUST state its choice as a Y-statement:

> In the context of `<use case or user story>`, facing `<concern>`, we decided for `<option>` to achieve `<quality>`, accepting `<downside>`.

A Decision MAY use the long form when the neglected options or the additional rationale carry real content:

> In the context of `<use case or user story>`, facing `<concern>`, we decided for `<option>` and neglected `<other options>`, to achieve `<quality>`, accepting `<downside>`, because `<rationale>`.

A Decision MAY add context, consequences, or alternatives considered below the Y-statement, only when they carry information the Y-statement cannot hold.

Alternatives considered MUST list only options that were genuinely evaluated.

A Decision SHOULD be created or updated when work changes:

- architecture
- dependencies
- security posture
- persistence
- deployment model
- agent workflow or skill

A Decision MUST support supersession. Supersession MUST be recorded from both ends, using the relationships defined in Section 11.1, and the superseded Decision MUST update its status.

## 9. Plans

Plan-specific metadata:

- `status`: `PlanStatus` [`required`, `enum`] - Plan lifecycle state. Values: `draft`, `ready`, `in-progress`, `paused`, `blocked`, `completed`, `abandoned`.

A Plan captures intended execution and handoff state.

A Plan MUST state its goal, its steps, and how the work is validated.

A Plan MAY also record a progress log, commands to run, and handoff notes, when those carry real content as defined in Section 6.1.

A Plan MUST define its validation steps before it reaches `in-progress`.

A Plan MUST be updated before work stops.

A large Plan SHOULD be split into phases to improve execution, review, or handoff clarity.

## 10. Evidence

Evidence-specific metadata:

- `status`: `EvidenceStatus` [`required`, `enum`] - Evidence lifecycle state. Values: `recorded`, `verified`, `contradicted`, `stale`.
- `result`: `EvidenceResult` [`required`, `enum`] - Validation result. Values: `pass`, `fail`, `partial`, `inconclusive`.

Evidence captures validation results.

Evidence MUST record what was checked and what happened.

Evidence MUST record the observed result of a check executed against the system. Authoring a document, creating a blueprint item, or restating that content exists is not a validation result and MUST NOT be recorded as Evidence.

Evidence MUST NOT claim correctness beyond the validation step it records.

Evidence MUST record who or what produced it, and SHOULD relate to the work it validates and the claims it supports, using the relationships defined in Section 11.1.

The producer requirement MAY be validated in a dedicated validation pass rather than at creation time. Validation MUST check that each Evidence item has at least one `produced-by` relationship to an actor; recording a producer outside such a relationship does not satisfy this requirement. When that check fails, the validator MUST report the failure, and the Evidence MUST NOT be treated as supporting any claim until the relationship exists. See Section 15 for general validation procedures.

Evidence MAY include:

- command output
- test results
- CI results
- review results
- benchmark results
- manual approval records

Agent-created Evidence SHOULD start in a recorded state.

Evidence MAY later be verified by CI, a human reviewer, or a separate review agent.

## 11. Relationships

Blueprints form a context graph. Relationships are the primary way to compose context: an item states what it relates to rather than restating what the related item owns.

Relationship targets MUST be represented as URI references.

Relationships to blueprint items MUST use the canonical `blueprint:<id>` URI and MUST resolve through the stable item ID rather than a file or storage path.

An `anchored-to` relationship MUST use the project-artifact target, immutable revision, and validation procedure defined in Section 5.3.

Relationship labels MUST be lower kebab case.

Relationships SHOULD be explicit when they affect context, review, validation, or future retrieval.

### 11.1 Relationship reference

This table defines every relationship a blueprint item may record. Read a row from either end: the `Label` column is what the `From` item records, and the `Reciprocal` column is what the `To` item records for the same relationship. The `Reason` column applies to both labels in the row.

| From     | To                  | Label             | Reciprocal       | Level  | Reason   | Use it when                                                                                                                                  |
| -------- | ------------------- | ----------------- | ---------------- | ------ | -------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| Spec     | Spec                | `related-to`      | `related-to`     | SHOULD | REQUIRED | either Spec provides direct, useful context for understanding the other; see Section 11.3                                                    |
| Spec     | Spec                | `depends-on`      | `required-by`    | SHOULD | REQUIRED | fulfilling the source's intent requires a specific capability or contract defined by the target; see Section 11.3                            |
| Decision | Spec                | `affects`         | `affected-by`    | SHOULD | OPTIONAL | the choice changes what the Spec means or requires                                                                                           |
| Decision | Decision            | `supersedes`      | `superseded-by`  | MUST   | OPTIONAL | the choice replaces an earlier one; see Section 8                                                                                            |
| Plan     | Spec                | `implements`      | `implemented-by` | SHOULD | OPTIONAL | the Plan executes toward that intent                                                                                                         |
| Plan     | Decision            | `implements`      | `implemented-by` | MAY    | OPTIONAL | the Plan exists to execute that choice                                                                                                       |
| Plan     | Decision            | `affected-by`     | `affects`        | MAY    | OPTIONAL | the choice constrains execution without affecting the Specs the Plan implements                                                              |
| Evidence | Plan                | `validates`       | `validated-by`   | SHOULD | OPTIONAL | the Evidence records that Plan's validation steps running                                                                                    |
| Evidence | Spec, Decision      | `supports`        | `supported-by`   | SHOULD | OPTIONAL | the observed result backs that item's claim                                                                                                  |
| Evidence | actor               | `produced-by`     | none             | MUST   | OPTIONAL | always; the target is the human, agent, CI system, or tool that produced the Evidence                                                        |
| Evidence | actor               | `reviewed-by`     | none             | MAY    | OPTIONAL | a human, agent, CI system, or tool reviewed the Evidence                                                                                     |
| any item | child item          | `decomposes-into` | `part-of`        | SHOULD | OPTIONAL | the item is intentionally broken down into smaller child items; see Section 6.1                                                              |
| any item | repository artifact | `anchored-to`     | none             | SHOULD | OPTIONAL | the item references a repository artifact at a specific commit, so the anchor resolves immutably; `target` and `revision` follow Section 5.3 |

Implementations MUST use only the labels defined in this table, in the directions it defines. Custom relationship labels are not allowed in this specification; extensions for other memory layers may be defined in later specifications.

`anchored-to`, `produced-by`, and `reviewed-by` have no reciprocal label because their targets are repository artifacts and actors outside the blueprint system, which cannot record a relationship back.

### 11.2 Recording both ends

When both ends of a relationship are blueprint items, the relationship SHOULD be recorded from both ends so each item carries the context a reader needs without loading the whole store. Section 8 makes this a requirement for supersession.

A Plan SHOULD reach Decision reasoning through the Specs it implements. A Plan MAY relate directly to a Decision only in the two cases Section 11.1 marks MAY.

### 11.3 Spec context and dependencies

`related-to` is symmetric and non-transitive. Its `reason` MUST identify the useful context the connection provides.

`depends-on` is directional, with `required-by` as its inverse. For either label, `reason` MUST identify the required capability or contract and why the dependent Spec needs it. The dependency's scope is the capability or contract identified in `reason`.

When a dependency exists, authors SHOULD use the dependency pair in preference to `related-to` for that connection.

When a Spec's intent changes, authors SHOULD review its incoming and outgoing Spec relationships and their reasons.

## 12. Conflict resolution

When information conflicts, the framework defines ownership. Section 6 defines what each blueprint item type owns. Two additional owners participate in conflict resolution:

- Code owns the current state (implementation).
- External sources own the original request or outside context that led to one or more blueprint items.

If code and specs disagree, the project has drift.

If evidence contradicts a completed plan, the plan MUST be revisited, or the contradiction MUST be explained.

If a decision is superseded, related specs SHOULD be reviewed for stale reasoning relationships.

## 13. Drift detection

A Blueprint system SHOULD detect drift.

Initial drift checks SHOULD include:

- code behavior differs from any applicable Spec
- intended behavior changed without a spec update
- dependency changed without a decision
- a relationship with a reciprocal label is recorded from only one end
- a completed plan defined validation steps whose results were never recorded
- failed evidence is related to completed work without explanation
- blueprint item relationships point to missing targets
- an item grew well beyond the size budget without being split

## 14. Blueprint impact

A change MAY declare no blueprint impact only when it does not change intent, behavior, architecture, dependency posture, security posture, operational behavior, documentation meaning, or agent workflow.

A no-impact claim SHOULD be validated.

A no-impact claim MUST be recorded with the change, using the change system's metadata, review notes, commit message, pull request description, or equivalent mechanism.

## 15. Validation and enforcement

Blueprints SHOULD be strict by default.

A project SHOULD validate blueprint consistency before accepting meaningful changes.

Validation MAY be performed by humans, agents, CI, or dedicated tooling.

A project SHOULD be able to block changes that violate required blueprint rules.

A validator MUST confirm that each item's `blueprints` field conforms to the `semver` constraint defined in Section 3.1 (e.g., `0.1.0-preview.5` is valid; `abc` is not).

A validator MUST check each relationship's label, direction, and fields against Sections 5.3 and 11.1.
