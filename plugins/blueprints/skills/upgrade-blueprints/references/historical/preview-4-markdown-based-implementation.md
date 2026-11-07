# Blueprints Markdown-Based Implementation Specification

Status: draft
Version: 0.1.0-preview.4

## 1. Purpose

This specification defines the Markdown-based implementation of Blueprints.

It defines how blueprint items are represented as Markdown files with machine-readable frontmatter and human-readable content, and how those files are organized in a feature-based folder structure.

A feature-based structure groups blueprint items by domain, feature, capability, or area of the solution, so humans and agents find related Specs, Decisions, Plans, and Evidence near each other.

## 2. Terminology

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://tools.ietf.org/html/rfc2119).

## 3. File format

Each blueprint item MUST be represented by a single Markdown file.

Each blueprint item file MUST include:

- YAML frontmatter for metadata.
- Markdown body content for the item content.

The frontmatter MUST appear before the Markdown body.

The Markdown body MUST contain the blueprint item's human-readable content.

## 4. File naming

The Markdown file name MUST match the blueprint item `id` with the `.md` extension.

Example: `plan.context-engine.index-dotnet-code` maps to `plan.context-engine.index-dotnet-code.md`

## 5. Item IDs

A blueprint item ID MUST start with the prefix for its item type:

- Specs MUST use `spec.`
- Decisions MUST use `decision.`
- Plans MUST use `plan.`
- Evidence MUST use `evidence.`

The prefix MUST match the item `type`.

After the required prefix, a blueprint item ID MUST contain one or more dot-separated namespace segments and MUST match the identifier grammar defined in Core Section 5.1. Dots separate namespace segments; hyphens separate words within a segment.

For a newly created item, namespace segments SHOULD initially reflect its feature-folder path so nearby items are recognizable. This is an initial naming convention, not the item's identity. Once assigned, an item ID and its matching file name MUST NOT change solely because a feature folder is renamed or moved.

An item ID SHOULD include enough domain context to remain globally clear.

Examples:

- `type: spec` uses `id: spec.context-engine.indexing` and file name is `spec.context-engine.indexing.md`.
- `type: decision` uses `id: decision.context-engine.use-neo4j` and file name is `decision.context-engine.use-neo4j.md`.
- `type: plan` uses `id: plan.context-engine.index-dotnet-code` and file name is `plan.context-engine.index-dotnet-code.md`.
- `type: evidence` uses `id: evidence.context-engine.index-dotnet-code-tests` and file name is `evidence.context-engine.index-dotnet-code-tests.md`.

## 6. Frontmatter

Frontmatter MUST be YAML delimited by `---` before and after the metadata block.

Frontmatter MUST contain all metadata required by the Core specification.

Frontmatter MAY contain optional metadata allowed by the Core specification.

Frontmatter field names MUST match the Core specification.

Every YAML mapping in frontmatter MUST contain unique field names. Duplicate mapping keys, including duplicate `id`, `type`, `status`, or `relationships` fields, are invalid and MUST be rejected before the metadata is accepted.

## 7. Folder structure

The artifact root MUST be `.blueprints/`.

`.blueprints/` MUST contain at least one Spec file directly at its root. A root Spec states the project's overall intent and is the entry point for discovery.

Feature folders SHOULD represent business domains, product areas, capabilities, features, or meaningful technical areas.

Feature folders MUST NOT represent blueprint item types. Avoid folder names such as `specs/`, `decisions/`, `plans/`, and `evidence/`; prefer names such as `security/`, `authentication/`, `context-engine/`, or `shopping-cart/`.

Feature folder names MUST use lower kebab case so their path segments can seed initial folder-level Spec IDs.

Feature folders MAY be nested when the domain or feature is large enough to benefit from subdivision. Small features MAY use a single folder.

For a newly created hierarchy, the initial naming convention can produce:

```text
.blueprints/
├─ spec.acme-platform.md
├─ security/
│  ├─ spec.security.md
│  ├─ decision.security.boundaries.md
│  └─ authentication/
│     ├─ spec.security.authentication.md
│     └─ magic-links/
│        ├─ spec.security.authentication.magic-links.md
│        ├─ decision.security.authentication.magic-links.md
│        ├─ plan.security.authentication.magic-links.md
│        └─ evidence.security.authentication.magic-links-tests.md
└─ integrations/
   ├─ stripe-billing/
   │  └─ spec.integrations.stripe-billing.md
   └─ sendgrid-email/
      └─ spec.integrations.sendgrid-email.md
```

In that hierarchy, `security/` and `authentication/` own shared intent and have folder-level Specs. `integrations/` groups two unrelated providers and owns no shared intent of its own, so it has no folder-level Spec, and each provider Spec relates to `spec.acme-platform` as described in Section 8.1.

Folder location helps discovery; relationships define meaning. Blueprint items MUST still relate to related items, sources, and anchors as required by the Core specification, and a blueprint item MAY relate to items in another feature folder.

## 8. Folder-level specs

A feature folder SHOULD include a folder-level Spec when the area owns shared intent or constraints that its items and subfolders do not each own individually. Shared intent reads as cross-cutting desired behavior, such as _every integration fails closed and retries with backoff_, rather than as a description of the children.

A folder-level Spec SHOULD NOT be created when its only content would list, name, or summarize the folder's children. Folder location and `decomposes-into` relationships already carry that, and Core Section 6.1 forbids an item that restates what a related item owns.

A feature folder that groups its children without owning shared intent MAY have no folder-level Spec.

When a folder-level Spec is first created, its ID SHOULD be derived from the feature folder's path relative to `.blueprints/`: prefix the path with `spec.` and replace each `/` path separator with `.` while preserving each lower-kebab-case folder segment. For example, a new folder-level Spec for `.blueprints/security/authentication/magic-links/` SHOULD initially use `id: spec.security.authentication.magic-links` and file name `spec.security.authentication.magic-links.md`.

After creation, the folder-level Spec's ID and file name MUST remain stable when the feature folder is renamed or moved. The file moves unchanged with the folder, existing `blueprint:<id>` relationship targets remain unchanged, and validators MUST NOT reject the item solely because its ID no longer mirrors the current folder path.

A folder-level Spec SHOULD be short, SHOULD define the scope and current desired state of the area, and SHOULD help humans and agents decide where to navigate next.

Child areas SHOULD be represented through `decomposes-into` relationships.

### 8.1 Ancestor reachability

A Spec stored in a feature folder MUST record a `part-of` relationship to the nearest ancestor folder-level Spec that owns it, or to a root Spec if no such Spec exists.

A root Spec records no `part-of` relationship.

This keeps every area reachable from a root Spec when an intermediate feature folder has no folder-level Spec of its own.

## 9. Discovery

Humans and agents SHOULD use this lookup order:

1. Match the task to the nearest feature or domain folder.
2. Read the folder-level Spec, when present.
3. When no folder-level Spec exists, read the Specs the folder contains and the nearest folder-level Spec in each descendant branch.
4. Follow child relationships when the task belongs to a subarea.
5. Read the most relevant child Spec.
6. Follow relationships to related decisions, plans, and evidence.
7. Search more broadly only when local discovery is not enough.

## 10. Examples

A Spec:

```markdown
---
id: spec.context-engine.indexing
type: spec
status: draft
summary: Index code into a graph so agents can retrieve focused context.
created: 2026-05-05
updated: 2026-05-05
blueprints: 0.1.0-preview.4
sources:
    - type: github-issue
      uri: https://github.com/org/repo/issues/123
relationships:
    - label: part-of
      target: blueprint:spec.context-engine
    - label: affected-by
      target: blueprint:decision.context-engine.use-neo4j
    - label: anchored-to
      target: file:src/ContextEngine/
      revision: git:0123456789abcdef0123456789abcdef01234567
---

# Spec: Context engine indexing

## Intent

The context engine indexes code so agents can retrieve focused context before editing.

## Current desired behavior

- The system identifies projects, files, types, methods, and package references.
```

A Decision, stating its choice as a Y-statement:

```markdown
---
id: decision.context-engine.use-neo4j
type: decision
status: proposed
summary: Store the context graph in Neo4j.
created: 2026-05-05
blueprints: 0.1.0-preview.4
relationships:
    - label: affects
      target: blueprint:spec.context-engine.indexing
---

# Decision: Use Neo4j for the context graph

In the context of retrieving focused context for agents, facing multi-hop traversals that relational joins express poorly, we decided for Neo4j to achieve fast relationship queries, accepting one more service to operate.
```

## 11. Validation

A Markdown-based Blueprint implementation SHOULD validate that:

- every item file has frontmatter
- every frontmatter mapping has unique field names, with duplicate keys rejected before metadata validation
- every item has required Core metadata, including `blueprints`
- every item's Markdown body contains non-empty content
- every item ID is unique
- every item ID matches the Core identifier grammar, checked before relationship targets are resolved
- every file name matches the item ID
- every item ID prefix matches the item type
- every new item uses the initial status defined by its type
- feature folders do not represent blueprint item types
- every feature-folder path segment uses lower kebab case, checked before a folder-level Spec ID is derived from that path
- `.blueprints/` contains at least one Spec file directly at its root
- every Spec in a feature folder records the `part-of` relationship required by Section 8.1, and that relationship resolves
- a stable item ID that no longer mirrors its current folder path is not treated as invalid
- every relationship target is a URI reference as defined by RFC 3986, every blueprint-item target uses the canonical `blueprint:<id>` form, and every `anchored-to` target and revision resolve using the Core project-artifact validation procedure
- every relationship label uses lower kebab case
- every relationship label, direction, and requirement level matches a row in the Core relationship reference
- every referenced blueprint item exists and resolves by stable item ID across feature folders
