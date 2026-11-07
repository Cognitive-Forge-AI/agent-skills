# Blueprints Legacy Execution Compatibility Specification

Status: draft
Version: 0.1.0-preview.6

## 1. Purpose

This frozen interpretation module preserves execution-history records authored under preview.4 and preview.5. It provides no authoring workflow and cannot be activated. Load it only when a project explicitly retains these definitions or a selected historical reference requires them. It is not a draft replacement extension.

## 2. Terminology

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://tools.ietf.org/html/rfc2119).

## 3. Provenance and selection

These definitions preserve the execution-related subset of Core at commit `4f6b1c655b829b5194a145cc3493553d45ec0475`, where execution types and labels were unchanged between preview.4 and preview.5. Earlier records require their historical upgrade steps first. Unrecognized versions remain an explicit interpretation limitation.

<!-- blueprints-module -->
```json
{
  "id": "legacy-execution",
  "version": "0.1.0-preview.6",
  "itemVersions": [
    "0.1.0-preview.4",
    "0.1.0-preview.5"
  ],
  "requires": [
    {
      "id": "project-memory",
      "mode": "reference-only"
    }
  ],
  "contributesTo": ["project-memory"],
  "referenceOnly": true
}
```

<!-- blueprints-types:start -->
| Type | Statuses | Initial | Approved | Required fields |
| --- | --- | --- | --- | --- |
| `plan` | `draft`, `ready`, `in-progress`, `paused`, `blocked`, `completed`, `abandoned` | `draft` | `ready` | none |
| `evidence` | `recorded`, `verified`, `contradicted`, `stale` | `recorded` | none | result=pass,fail,partial,inconclusive |
<!-- blueprints-types:end -->

The initial/approved columns interpret historical lifecycle states rather than permitting new records to be created or approved. Existing items retain their authored version and all recorded content.

## 4. Retained relationship definitions

The table follows [Core §7](preview-6-core.md#7-core-relationships). Labels may occur on active items when referring to retained targets, and reciprocal directions still mean the same connection. Core structural relationships remain available.

<!-- blueprints-relationships:start -->
| From | To | Label | Reciprocal | Level | Reason | Reciprocal rule | Use it when |
| --- | --- | --- | --- | --- | --- | --- | --- |
| plan | spec | `implements` | `implemented-by` | SHOULD | OPTIONAL | SHOULD | The historical work executes toward this intent. |
| plan | decision | `implements` | `implemented-by` | MAY | OPTIONAL | SHOULD | The historical work executes this choice. |
| plan | decision | `affected-by` | `affects` | MAY | OPTIONAL | SHOULD | The choice constrains execution without affecting its implemented Specs. |
| evidence | plan | `validates` | `validated-by` | SHOULD | OPTIONAL | SHOULD | The observation records this work record's validation steps running. |
| evidence | spec, decision | `supports` | `supported-by` | SHOULD | OPTIONAL | SHOULD | The observed result backs the historical claim. |
| evidence | actor | `produced-by` | none | MUST | OPTIONAL | none | Always: identify the human, agent, CI system, or tool that produced the observation. |
| evidence | actor | `reviewed-by` | none | MAY | OPTIONAL | none | A human, agent, CI system, or tool reviewed the observation. |
<!-- blueprints-relationships:end -->

## 5. Historical semantics and limits

A Plan records intended execution and handoff, including its goal, steps, and validation approach. An Evidence item records an executed check and observed outcome, with a `result` value from its definition. It MUST have a `produced-by` relationship to an actor before it can be treated as support for a claim. Merely authoring documents did not establish validation.

The `affects` / `affected-by` execution row is a frozen historical endpoint signature of Project Memory's existing choice-influence vocabulary. It does not grant future extensions permission to add endpoint signatures. The composer recognizes this one compatibility contribution explicitly, with provenance, and MUST NOT treat it as a second independent owner or as a general collision exception.

Records and all their existing links remain unchanged. New references from active items MUST identify a valid connection to the historical claim and MUST NOT imply that changed current behavior was checked. Interpret an observation in its original scope. [Core §7](preview-6-core.md#7-core-relationships) governs reverse links into these retained records, whether recommended or mandatory.

There is no automatic status maintenance, rerun, producer invention, completion workflow, or assertion of current correctness. Report missing definitions, invalid records, and unresolved targets without rewriting them. To remove this module, first resolve retained items, remaining links, and dependencies.
