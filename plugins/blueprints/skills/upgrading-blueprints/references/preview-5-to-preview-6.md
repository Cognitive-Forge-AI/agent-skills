# Upgrade to Blueprints 0.1.0-preview.6

This catalog upgrades preview.4/preview.5 project memory in place, preserving supported items and retained history. Invoke it only for an explicit upgrade request. It requires no archiving or deletion.

## 1. Inventory and baseline

Use bounded discovery to inventory IDs, types, authored versions, paths, approvals, and relationship endpoints before editing. Capture a trusted baseline outside the active store — a Git revision or a temporary copy — for the comparison in step 6. Capturing it neither replaces the store nor archives anything.

Read the selected target [Core](historical/preview-6-core.md), [Markdown binding](historical/preview-6-markdown-based-implementation.md), and [Project Memory](historical/preview-6-project-memory.md). Load [frozen legacy definitions](historical/preview-6-legacy-execution.md) only if the inventory contains their records or uses their vocabulary.

Preview.1 records need their existing historical upgrade first. Leave unknown source versions and types unchanged, and report the limitation explicitly. Do not run the preview.4 stamping scripts on this modular transition or on newer mixed stores.

## 2. Declare adoption

Create `.blueprints/config.json` selecting the target suite once through `blueprints` and Project Memory active. Extension entries contain only `id` and `mode`. If historic execution types or their relationships exist, also select the reference-only compatibility definitions:

```json
{
  "blueprints": "0.1.0-preview.6",
  "extensions": [
    {"id": "project-memory", "mode": "active"},
    {"id": "legacy-execution", "mode": "reference-only"}
  ]
}
```

Omit the second selection when it is not used, and infer activation for no other capability. Retention preserves how historical items are interpreted without creating or advancing any of them.

## 3. Upgrade supported memory in place

Keep Spec and Decision paths, IDs, bodies, creation dates, source provenance, valid approvals, and valid relationships. Do not reset accepted or active items to initial statuses, because changing an item's representation does not create it.

For preview.4 Specs at `replaced`, apply the intent/approval resolution in the [preview.4 → preview.5 catalog](preview-4-to-preview-5.md#1-resolve-the-retired-spec-status). Apply its Spec reason requirements where those labels occur. Other preview.4/preview.5 memory already uses the retained status vocabulary. Make only required conformance corrections supported by the item's meaning and project authority.

Review ownership ancestry, including local child Specs and grouping folders. Do not rename IDs to mirror folders. Do not drop or translate valid historical relationships to ordinary sources merely because their target is retained — the compatibility module preserves the original relationship semantics and the old target's version.

After required structural and semantic checks establish conformance, update each supported item's `blueprints` field to preview.6. You may check a candidate diff or temporary candidate copy before applying a stamp. Leave unresolved items at their prior version and report an incomplete upgrade. Never infer success from the stamp alone.

## 4. Retain historical records

Keep Plan and Evidence records byte-for-byte unchanged, including their preview.4/preview.5 versions, statuses, producers, content, and all edges. These types leave active authoring, not the identity index. Do not synthesize reciprocals in retained files or rerun their workflows. Missing required data stays a diagnostic, and never becomes permission to fabricate it.

Active Specs and Decisions can refer to retained records under the frozen definitions, but an old observation does not validate behavior that has since changed. A reciprocal that would require editing retained history is governed by [Core §7](historical/preview-6-core.md#7-core-relationships): stop that connection edit and resolve the reference or policy choice separately.

## 5. Optional cleanup

Retention in place is the default. If the user requests cleanup, inventory inbound references before proposing exact moves or deletions.

For selective archiving, use repository-contained disjoint archive roots declared in `config.json`. Move only authorized retained items, preserve IDs and bytes, and verify index resolution across active and archive roots. Marking history reference-only does not require moving it out of a feature-based active store.

For deletion, obtain explicit authorization for the identified items and affected references. Resolve inbound links as part of the same change, rather than leaving a future reader to discover a broken target.

## 6. Verify and report

- Compare supported items with the baseline: identities, locations, content, approvals, provenance, and valid links remain intact except for documented required corrections/version stamps.
- Compare retained files byte-for-byte; there must be no changes or missing records unless separately authorized cleanup was performed.
- Check the selected suite, available definitions, recorded item versions, metadata, targets, relationship reasons/directions, required reciprocals, Spec ancestry, and immutable anchors. Missing historical definitions are an explicit limitation.
- Run the repository's relevant checks. In a framework checkout, `node scripts/check-blueprints.mjs --root <store> --json` provides focused structural checks. `--before <baseline-store>` checks item mutations; a pre-modular baseline may lack config. `--no-git` is incomplete when anchors exist. These development scripts are not assumed installed with the skill; use the definitions directly if unavailable.
- Inspect semantic intent and approval separately. A snapshot check cannot prove an authorized transition.
- Report changed supported items, unchanged retained records, advisory findings, incomplete checks, and unresolved items. Re-running the completed upgrade must make no changes.
