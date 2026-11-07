# Blueprints 0.1.0-preview.4 to 0.1.0-preview.5

Apply this step by reviewing and editing preview.4 items. The authoritative definitions are in the bundled [Core specification](historical/preview-5-core.md).

## 1. Resolve the retired Spec status

`SpecStatus` now contains `draft`, `active`, and `removed`. For each Spec at `replaced`, inspect its content, related items, and change history to determine which state applies:

| Meaning | Action |
| --- | --- |
| Its intent has been withdrawn | Set `status: removed`. |
| The item still owns current intent | Update the same item; use `active` when that intent has explicit approval, otherwise `draft`. |
| Its intent or approval is unclear | Ask the project approver and leave the item at preview.4 pending resolution. |

Preserve the item's stable ID and provenance. Review inbound relationships against the resulting intent.

## 2. Review Spec connections

Review any provisional `related-to`, `depends-on`, and `required-by` relationships against [Core Section 11.3](historical/preview-5-core.md#113-spec-context-and-dependencies). When the Specs establish another useful connection, record it using those definitions.

For each connection, confirm both endpoints are Specs and supply a short `reason` grounded in their content. Record the reciprocal under [Core Section 11.2](historical/preview-5-core.md#112-recording-both-ends), with a reason consistent with the same connection.

For example, an audit logging Spec can record:

```yaml
- label: depends-on
  target: blueprint:spec.identity
  reason: Requires authenticated actor identity to attribute audit events.
```

The identity Spec records:

```yaml
- label: required-by
  target: blueprint:spec.audit-logging
  reason: Provides the authenticated actor identity used to attribute audit events.
```

Where an existing reason or label conflicts with the Specs' intent, resolve the meaning before changing the relationship. Follow the existing approval process for any resulting change to intent.

## 3. Update the version

After resolving an item's required changes, update its framework version:

```diff
-blueprints: 0.1.0-preview.4
+blueprints: 0.1.0-preview.5
```

Keep unresolved items at their original version and report what remains. Update installed Blueprints references and repository instructions that name the framework version to preview.5 as part of the upgrade.

## 4. Verify

- Check each upgraded Spec uses a status from Core Section 7 and retains its approval meaning.
- Validate relationship fields under Core Section 5.3 and labels, endpoint types, and reason requirements under Section 11.1.
- Review reasons against the connected Specs and check reciprocal entries for consistency.
- Confirm each upgraded item is stamped preview.5 and stable IDs, sources, and creation timestamps are preserved.
- Review the diff and run the repository's relevant checks. Report unresolved items and missing recommended reciprocals separately.
