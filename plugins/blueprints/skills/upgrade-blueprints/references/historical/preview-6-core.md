# Blueprints Core Specification

Status: draft
Version: 0.1.0-preview.6

## 1. Purpose

Blueprints is a strict-by-default, minimal-by-design methodology for building graph-ready project memory with humans and AI agents.

Core defines identity, shared metadata, graph mechanics, and how consumers interpret adopted definitions. It defines no item types. Projects choose the extensions that do, and required rules stay strict within the chosen capabilities.

## 2. Terminology

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://tools.ietf.org/html/rfc2119).

## 3. Shared types and constraints

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

## 4. Common model

A blueprint item owns one kind of meaning and has machine-readable metadata and non-empty content. Type definitions determine content and lifecycle requirements. Authored memory and derived graph entities are distinct: a code symbol or commit MAY be an external graph entity without becoming an authored Blueprint item.

<!-- blueprints-module -->
```json
{
  "id": "core",
  "version": "0.1.0-preview.6",
  "itemVersions": [
    "0.1.0-preview.6"
  ],
  "requires": []
}
```

## 5. Metadata

Every item MUST include:

- `id`: `Identifier` [`required`, `unique`, `stable`, `non-empty`] - Stable blueprint item identifier.
- `type`: `ItemType` [`required`, `enum`] - Blueprint item type. Values are defined by selected extensions.
- `status`: `Enum` [`required`, `enum`] - Lifecycle state using the status values defined by the item's type and recorded framework version.
- `summary`: `String` [`required`, `non-empty`, `short`] - Relevance preview for humans and agents.
- `created`: `Timestamp` [`required`] - Creation date or timestamp.
- `blueprints`: `String` [`required`, `non-empty`, `semver`] - Blueprints framework version whose definitions the item conforms to; retain historical versions until an explicit upgrade establishes conformance.

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

Relationship fields:

- `label`: `String` [`required`, `non-empty`, `lower-kebab-case`] - Relationship meaning supplied by a selected definition.
- `target`: `URI` [`required`, `resolvable`] - Target identity. Item targets MUST use the canonical `blueprint:<id>` URI.
- `reason`: `String` [`non-empty`, `short`] - Purpose of the connection; the relationship definition controls whether it is required.
- `revision`: `String` [`immutable`] - Target revision; REQUIRED for `anchored-to` and OPTIONAL otherwise unless a definition requires it.

A relationship MUST match a selected definition's label, direction, endpoint types, required fields, and reciprocal rule. Definitions from a reference-only extension remain available for interpreting history and for valid new references from active items. Selecting vocabulary does not establish the truth of a claim.

`anchored-to` identifies an artifact at an immutable revision. The storage binding defines its concrete target and revision syntax and resolution. A validator MUST NOT substitute current artifact content for the recorded revision.

## 6. Project adoption

A project MUST declare its suite version and extension selections. Each selection identifies a definition module and one mode: `active` or `reference-only`. Because the project suite version selects all definitions together, selections MUST NOT specify independent extension versions. Core is implicit. The Markdown representation is defined by the storage binding.

An active extension permits new items and normal workflows. A reference-only extension permits reading existing records and adding valid historical references from active items. Ordinary work MUST NOT create, modify, or advance reference-only items, and MUST NOT reactivate their workflows. Explicitly authorized historical correction or reactivation is a separate project operation.

Mode is project policy, not item status. Changing mode MUST NOT rewrite any item metadata, content, or relationships, and the storage binding defines exactly what representation preservation means. An item's type and recorded version select its interpretation. A module's published version MUST match the selected suite release, while its declared item versions identify records that set can interpret.

Required dependencies MUST resolve. A dependency requiring `active` cannot be satisfied by `reference-only`. A dependency requiring `reference-only` needs the definition to be available and accepts either mode. Dependency cycles, duplicate identities, ambiguous label ownership, and unavailable definition versions MUST be reported. A consumer MUST NOT infer success from a partial definition set.

Retiring authoring MAY keep existing items in place. Removing definitions entirely MUST first resolve remaining references, owned items, and dependencies. Neither operation requires wholesale store archiving.

Projects choose rules; tasks choose relevant context. A task MUST NOT silently change project selections to weaken applicable rules. Reference-only bodies SHOULD be loaded only for an explicit historical question or a relevant followed connection.

## 7. Core relationships

Read each row from either end. `Label` is stored by `From`, and `Reciprocal` by `To`. `Reason` applies to both directions. `Level` governs recording when the stated condition holds. `Reciprocal rule` governs recording the opposite end once a connection exists. `item` means any item with available type definitions. `artifact` and `actor` are external endpoint categories.

<!-- blueprints-relationships:start -->
| From | To | Label | Reciprocal | Level | Reason | Reciprocal rule | Use it when |
| --- | --- | --- | --- | --- | --- | --- | --- |
| item | item | `decomposes-into` | `part-of` | SHOULD | OPTIONAL | SHOULD | The source is intentionally decomposed into the target child. |
| item | artifact | `anchored-to` | none | SHOULD | OPTIONAL | none | The source identifies a project artifact at an immutable revision. |
<!-- blueprints-relationships:end -->

Every pair has one owner. A reciprocal MUST preserve the meaning of the same connection. Required reciprocal records MUST NOT be waived merely because the target is reference-only. If satisfying a new connection would require changing a retained item, ordinary work MUST stop that edit and report the constraint. The project can then choose another permitted reference or authorize a separate policy change. Missing recommended reciprocals are advisories, not permission to edit retained items.

## 8. Validation

Validate applicable rules before accepting meaningful changes. Required violations prevent a conformance claim. Report advisory findings separately. Unknown definitions, truncated discovery, or unchecked revisions make validation incomplete, not successful.

Snapshot checks establish current structure. Checking that no retained item was created or modified, or that a lifecycle transition had approval, requires a previous snapshot or an authoritative change/approval record. Without it, a validator MUST report its limited scope and MUST NOT claim transition compliance.

Historical records use their recorded versions. A new reference to an old observation does not show that current behavior was checked. Keep historical interpretation separate from current correctness and review connections when the source's meaning changes.

Validation reports belong outside item content. A checker MUST NOT rewrite items merely to report its findings. Semantic judgment about intent, reasoning, approval, and drift remains necessary after structural checks pass.
