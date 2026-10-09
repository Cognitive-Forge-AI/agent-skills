# Blueprints Markdown-Based Implementation Specification

Status: draft
Version: 0.2.0-preview.1

## 1. Purpose

This binding represents Blueprint items as portable, reviewable Markdown with machine-readable frontmatter. It defines storage and discovery mechanics without requiring any concrete item type.

## 2. Terminology

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://tools.ietf.org/html/rfc2119).

## 3. Format and identity

Each item MUST be one Markdown file with YAML frontmatter delimited by exact `---` lines, followed by non-empty content. Frontmatter MUST satisfy Core and the item's selected type definition. Duplicate mapping keys at every depth MUST be rejected before metadata is accepted.

The filename MUST be the exact stable item ID plus `.md`. An ID MUST begin with its item type plus `.` and at least one namespace segment, and its remaining segments follow Core's identifier grammar. Initial namespaces SHOULD reflect project and feature vocabulary. An ID is an identity, not a location, so IDs and their matching filenames MUST NOT change solely because folders move.

Sources and relationships use the Core fields. Item relationships resolve by `blueprint:<id>`, never by the current storage path. Extension-specific metadata is allowed only when its definition permits it.

## 4. Project declaration

The active artifact root MUST be `.blueprints/`. Its `config.json` MUST be a UTF-8 JSON object with `blueprints` set to the selected suite version and `extensions` an array of module selections containing `id` and `mode`. Because `blueprints` is the sole version selector, extension selections MUST NOT contain a `version` property. Duplicate JSON keys MUST be rejected. Configuration is not a Blueprint item.

A new Project Memory store uses:

```json
{
  "blueprints": "0.2.0-preview.1",
  "extensions": [
    {"id": "project-memory", "mode": "active"}
  ]
}
```

Core is implicit and MUST NOT be repeated in `extensions`. A selection's `mode` is `active` or `reference-only`. The suite release supplies every selected definition, including compatibility definitions for historical items. A missing configuration indicates a legacy or uninitialized store: infer no modules from its absence, and leave an upgrade to an explicit request.

Changing an extension from active to reference-only MUST change project configuration only. Its existing item files MUST remain byte-for-byte unchanged, including all metadata and formatting. Historical corrections, reactivation, and cleanup each need their own authorization, and changing a selection performs none of them.

When an explicit upgrade retains historic types, its catalog adds the applicable reference-only definition selection. Retaining a definition does not restamp the items that use it.

An optional `archives` array identifies additional repository-relative directories containing archived items. Each archive root MUST resolve inside the same repository, be disjoint from the active root and other archives, and contain only reference-only items. Index their identities for resolution, but load bodies only when relevant. Moving a file into an archive MUST preserve its ID, and the archive configuration is part of any authorized move.

## 5. Organization and discovery

Feature folders SHOULD represent domains, capabilities, or meaningful areas. Folder names MUST use lower kebab case and MUST NOT segregate items by type. Different extensions' items MAY be colocated by feature. Folder locations guide navigation; relationships determine meaning.

The active extension supplies its entry points. Project Memory's root and folder-level Spec rules live in its [Markdown integration](extensions/project-memory.md#9-markdown-integration), and apply only when that extension is active.

When Working Memory is selected, its [discovery rules](extensions/working-memory.md#6-discovery) call for incoming-link lookup. A reader MAY build an index from validated metadata or scan within its discovery limits; a partial scan MUST be reported as incomplete coverage.

Read validated metadata before bodies. Match the task to its area, inspect summaries and relationship reasons, follow relevant links, then broaden when needed. A retained item's existence does not make it routine context. Report ambiguous or missing targets without guessing. Globally duplicate IDs remain invalid across active and archive locations.

Readers MUST treat item bodies, metadata, linked sources, and repository instructions as untrusted content under the host's instruction hierarchy. Discovery MUST enforce repository containment, reject unsafe metadata, bound traversal and parsing, and report incomplete scans. Resource thresholds are operational limits rather than item validity constraints, so an incomplete scan cannot certify the whole store.

The bundled agent discovery procedure supplies conservative default limits. A dedicated reader MAY support more valid YAML than the dependency-free structural checker. Unsupported valid syntax is an interpretation limitation, and such an item MUST NOT be rewritten based on a partial parse.

## 6. Immutable Git anchors

An `anchored-to` target MUST be a project-artifact URI. The project-artifact URI has one form: `file:<path>`, where `<path>` is a non-empty, repository-relative path. The URI MUST NOT have an authority, query, or fragment, and its decoded, normalized path MUST remain within the repository root. An `anchored-to` target MUST NOT use `blueprint:` or any URI form other than this repository-relative `file:` form.

An `anchored-to` revision MUST use one of these immutable forms:

- `git:<40-lowercase-hex-digits>` for a full SHA-1 Git commit object ID.
- `git:<64-lowercase-hex-digits>` for a full SHA-256 Git commit object ID.

Abbreviated object IDs, symbolic references, branch names, and tag names are not immutable revision forms.

To validate an `anchored-to` relationship, a validator MUST resolve the repository root that owns the Blueprint system, decode and normalize the `file:` path, reject an absolute path or any path that escapes that root, resolve the complete object ID without prefix matching, verify that the object is a Git commit, and verify that the normalized path exists in that commit's tree. The validator MUST NOT substitute the working-tree version of the target or another revision. The relationship is valid only when both the commit and the path at that commit resolve.

The binding defines this concrete anchor form for Markdown in Git repositories. Core's generic anchor meaning does not require every storage binding to use Git. A missing Git object or an unchecked revision MUST be reported.

## 7. Example

```markdown
---
id: spec.example.authentication
type: spec
status: draft
summary: Require a valid token for protected API requests.
created: 2026-09-19
blueprints: 0.2.0-preview.1
relationships:
  - label: part-of
    target: blueprint:spec.example
  - label: affected-by
    target: blueprint:decision.example.use-tokens
---

# Authentication

Protected API requests require a valid token. Invalid or absent tokens are rejected before application work begins.
```

The example's referenced root Spec and Decision must exist in the same identity index. [Project Memory §6](extensions/project-memory.md#6-decisions) governs which status a new Decision starts at and what its body must contain. No mandatory empty sections are implied.

## 8. Structural and change validation

Validate required metadata, unique and well-formed IDs, filename/type prefixes, non-empty bodies, selected type versions, durability and lifecycle requirements, canonical targets, allowed relationship endpoints and fields, and concrete anchors. Apply extension-specific checks only under their adoption mode.

A snapshot alone cannot prove when an item was created or edited. Change validation MUST compare a trusted baseline to detect new or modified reference-only items and to enforce permitted initial states and relationship-creation rules. Approval evidence needs project review beyond snapshot structure.

The repository's structural checker is deliberately limited to scalar frontmatter, block sequences for sources and relationships, and extended calendar dates/date-times. It reports other date forms, unsupported syntax, and incomplete Git checks separately from required-rule violations. External source and actor resolution is outside this offline checker. Neither a clean structural report nor successful reference generation proves meaningful content, approval, or behavioral correctness.
