# Blueprints 0.1.0-preview.1 to 0.1.0-preview.4

Interpret this historical step using its frozen [preview.4 Core](historical/preview-4-core.md) and [Markdown binding](historical/preview-4-markdown-based-implementation.md). Newer modular definitions do not change this transition.

Every difference between the two framework versions that can invalidate stored project memory, with the fix for each.

`Class` says who applies the fix:

- **mechanical** — `scripts/fix.mjs` rewrites it. The result is fully determined by the store and, where relevant, by git.
- **agent** — the fix needs the item's meaning, a history choice, or a user answer. The scripts report it and never touch it.

`Code` is the finding code `scan.mjs` prints, so a report line maps back to the section that explains it.

| #   | Change                                              | Class      | Code                                                                                                                                                                              |
| --- | --------------------------------------------------- | ---------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Framework version stamp                             | mixed      | `version-stamp`, `missing-blueprints-field`                                                                                                                                       |
| 2   | Plan status `active` removed                        | mechanical | `plan-status-active`                                                                                                                                                              |
| 3   | Canonical `blueprint:<id>` targets                  | mixed      | `target-to-canonical`, `target-trailing-extension`, `unresolvable-target`                                                                                                         |
| 4   | `supported-by` changed meaning                      | mixed      | `supported-by-remap`, `supported-by-ambiguous`                                                                                                                                    |
| 5   | `anchored-to` revision required and immutable       | mixed      | `anchor-revision-expand`, `anchor-revision-missing`, `anchor-revision-symbolic`, `anchor-revision-abbreviated`, `anchor-revision-unresolvable`, `anchor-path-missing-at-revision` |
| 6   | `anchored-to` target must be repository-relative    | mixed      | `anchor-target-scheme`, `anchor-target-invalid`                                                                                                                                   |
| 7   | Reciprocal relationships recorded from both ends    | mechanical | `missing-reciprocal`                                                                                                                                                              |
| 8   | Identifier grammar                                  | agent      | `invalid-id`, `missing-id`, `duplicate-id`, `unknown-type`, `filename-mismatch`, `prefix-mismatch`                                                                                |
| 9   | Closed relationship label set and directions        | agent      | `unknown-label`, `direction-invalid`, `non-item-target`, `incomplete-relationship`                                                                                                |
| 10  | Decisions state a Y-statement                       | agent      | `decision-missing-y-statement`                                                                                                                                                    |
| 11  | Specs are present tense, not narratives             | agent      | `spec-reads-as-narrative`                                                                                                                                                         |
| 12  | Evidence records an executed check and its producer | agent      | `evidence-missing-produced-by`, `evidence-no-observed-result`                                                                                                                     |
| 13  | Body size budget                                    | agent      | `body-over-budget`                                                                                                                                                                |
| 14  | Plans and Evidence only when meaningful             | agent      | not detected; see below                                                                                                                                                           |
| 15  | Frontmatter and body hygiene                        | agent      | `duplicate-key`, `empty-body`, `unsupported-frontmatter`, `missing-frontmatter`, `unterminated-frontmatter`, `invalid-utf8`, `file-too-large`, `unreadable-file`                  |

`mixed` means the change produces both kinds: the determined cases are rewritten, and the rest are reported.

## 1. Framework version stamp

preview.1 required the `blueprints` field on every item and made updating it mandatory when migrating across a breaking version. preview.4 keeps the field and requires valid semver, and this upgrade is exactly such a migration.

```diff
-blueprints: 0.1.0-preview.1
+blueprints: 0.1.0-preview.4
```

An item with no `blueprints` field at all is reported as `missing-blueprints-field` rather than stamped. It was already required under preview.1, so its absence means the item may not belong to this store; confirm before adding one.

## 2. Plan status `active` removed

preview.1 `PlanStatus`: `active`, `paused`, `blocked`, `completed`, `abandoned`.
preview.4 `PlanStatus`: `draft`, `ready`, `in-progress`, `paused`, `blocked`, `completed`, `abandoned`.

```diff
 type: plan
-status: active
+status: in-progress
```

`draft` and `ready` are the new pre-execution states. A Plan that was `active` under preview.1 was already executing, so `in-progress` is the equivalent. **Never** rewrite it to `draft` — that would silently withdraw an approval.

Spec, Decision, and Evidence status values are unchanged.

**Do not downgrade an existing status.** Core section 5.4 constrains what status an item may be _created_ at. It does not retroactively invalidate a Spec that is already `active` or a Decision that is already `accepted`.

## 3. Canonical `blueprint:<id>` targets

preview.1 allowed any RFC 3986 URI reference, and its own example used a storage-relative path. preview.4 requires the canonical form, resolved by item ID rather than by path, so a target survives the item being renamed or moved.

```diff
 relationships:
     - label: implemented-by
-      target: plan.index-dotnet-code.md
+      target: blueprint:plan.index-dotnet-code
```

`fix.mjs` resolves a path target through the file basename, which is the only link a preview.1 store has between a path and a stable ID. It rewrites **only** on an unambiguous match. A path matching two files, or none, is reported as `unresolvable-target` and left alone.

A `blueprint:<id>` target naming an ID that more than one file carries is reported the same way: the ID does not identify a single item, so nothing is rewritten and no reciprocal is recorded from it. Fix the duplicate IDs first.

`anchored-to` is never rewritten this way — it points at a repository artifact, not at an item. See change 6.

## 4. `supported-by` changed meaning

This is the one silent semantic change in the release, and the one most likely to survive an upgrade looking correct.

- preview.1: `supported-by` meant "this Spec is supported by that Decision". A Spec recorded it, pointing at a Decision.
- preview.4: `supported-by` is the reciprocal of Evidence `supports`. A Spec or Decision records it, pointing at **Evidence**.

The preview.4 relationship for the old meaning is `affected-by`, whose reciprocal is `affects` on the Decision.

```diff
 relationships:
-    - label: supported-by
-      target: decision.use-neo4j.md
+    - label: affected-by
+      target: blueprint:decision.use-neo4j
```

`fix.mjs` remaps `supported-by` only when the resolved target is a Decision. A `supported-by` pointing at Evidence is already correct under preview.4 and is left alone. Anything else is reported as `supported-by-ambiguous` for reclassification by hand.

## 5. `anchored-to` revision is required and immutable

preview.1: `revision` was optional on every relationship, with no required form. Its example used `revision: abc123`.

preview.4: `revision` is REQUIRED when `label` is `anchored-to`, and must be `git:` followed by a full 40-hex SHA-1 or 64-hex SHA-256 commit object ID. Abbreviated IDs, branch names, and tag names are not immutable revision forms.

```diff
     - label: anchored-to
       target: file:src/ContextEngine/
-      revision: abc123
+      revision: git:0123456789abcdef0123456789abcdef01234567
```

Validation resolves the complete object ID without prefix matching, verifies the object is a commit, and verifies the normalized path exists in **that commit's tree** — never the working tree.

| Stored revision                                 | Handling                                                                                      |
| ----------------------------------------------- | --------------------------------------------------------------------------------------------- |
| `git:` plus a full 40- or 64-hex object ID      | already correct; verified, never rewritten                                                    |
| Hex that resolves, path present                 | `anchor-revision-expand` — mechanical: rewritten to `git:<full oid>`                          |
| Hex that does not resolve                       | `anchor-revision-unresolvable` — ambiguous, from another repository, or unreachable           |
| Resolves, but the path is absent at that commit | `anchor-path-missing-at-revision` — the anchor names a commit that never contained the target |
| Hex, but no git repository was available        | `anchor-revision-abbreviated` — nothing could expand or verify it; supply the full object ID  |
| Branch name, tag, or symbolic ref               | `anchor-revision-symbolic` — pick the commit it meant at authoring time                       |
| A ref whose name is all hex digits              | `anchor-revision-symbolic` — see below                                                        |
| Absent                                          | `anchor-revision-missing` — decide which commit the anchor pins to                            |

A full object ID written without the `git:` prefix is hex like any other: it is immutable already, so it takes the same path and gains the prefix. `--no-git` reports it as `anchor-revision-abbreviated` along with the genuinely abbreviated ones, because with no repository to consult the two cannot be told apart.

A branch or tag can be named with nothing but hex digits — `cafe`, `deadbeef`, or `abc123`, which is the revision preview.1's own example used. Such a name is indistinguishable from an abbreviated commit ID by shape, so the scripts check the ref namespaces first and report it as symbolic. Expanding it would pin the anchor to wherever that ref points today, which is the mutable revision preview.4 set out to forbid.

For the last five, choose the commit from the item's own history: `git log --diff-filter=A -- <path>` for when the anchor's target first appeared, or the commit that the Plan or Evidence around the item refers to. Do not default to `HEAD` because it is convenient — an anchor pointing at today's commit claims something the item never said.

`revision` remains optional on every other label.

## 6. `anchored-to` target must be repository-relative

preview.4 gives the anchor target exactly one form: `file:<path>`, where `<path>` is a non-empty repository-relative path with no authority, query, or fragment, whose normalized form stays inside the repository root.

```diff
     - label: anchored-to
-      target: src/ContextEngine/
+      target: file:src/ContextEngine/
```

Adding the `file:` scheme is mechanical. An absolute path, a `file://` authority, a query or fragment, or a path escaping the repository is reported as `anchor-target-invalid` — each means the anchor has to be re-pointed, not reformatted.

## 7. Reciprocal relationships from both ends

preview.4 section 11.2 says a relationship between two blueprint items SHOULD be recorded from both ends, so each item carries the context a reader needs without loading the whole store. Supersession has required both ends since preview.1. A one-sided relationship is now a drift signal.

| Label             | Reciprocal       |
| ----------------- | ---------------- |
| `affects`         | `affected-by`    |
| `supersedes`      | `superseded-by`  |
| `implements`      | `implemented-by` |
| `validates`       | `validated-by`   |
| `supports`        | `supported-by`   |
| `decomposes-into` | `part-of`        |

`anchored-to`, `produced-by`, and `reviewed-by` have no reciprocal: their targets sit outside the blueprint system and cannot record anything back.

`fix.mjs` inserts the missing half into the target item, matching that file's existing indentation, and creates a `relationships:` block if the item has none. It skips a pair that is already recorded from both ends.

## 8. Identifier grammar

preview.1 required IDs to be stable, unique, and human-readable, and recommended lower kebab case after the type prefix. preview.4 makes it a grammar a validator MUST enforce, checked **before** relationships resolve so a malformed target is reported as a bad identifier rather than as a missing item.

```abnf
id       = segment *( "." segment )
segment  = alphanum *( [ "-" ] alphanum )
alphanum = %x61-7A / %x30-39   ; lowercase ASCII letter or digit
```

A segment must not be empty and must not begin or end with a hyphen. Uppercase letters, underscores, spaces, and doubled hyphens are all rejected.

This is left to the agent because a rename cascades: the ID, the file name, and every inbound `blueprint:` target across the store all move together, and the ID is supposed to be stable. Work one item at a time, and confirm the rename with the user before touching a store other people also read.

**Do not rename a grammar-valid ID.** preview.4 introduces the convention that a new item's namespace segments mirror its feature-folder path, but it also states that an ID must not change because a folder was renamed or moved. An existing valid ID stays exactly as it is.

Identity is checked as a whole, so three more codes are reported here:

| Code           | Meaning                                                                                                                     |
| -------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `missing-id`   | No `id` field. Core section 5 requires one; recover the intended ID from the file name and the item's content.              |
| `duplicate-id` | Two or more files claim the same ID. IDs must be unique, and inbound targets cannot resolve until one of them changes.      |
| `unknown-type` | `type` is not `spec`, `decision`, `plan`, or `evidence`. Decide which of the four the item is; the ID prefix moves with it. |

Each one blocks relationship resolution for the item, so fix these before working the relationship findings.

## 9. Closed relationship label set and directions

preview.1 listed ten standard labels and forbade custom ones. preview.4 replaces that list with a table that defines, for each relationship, the item type at each end and the requirement level.

| From     | To                  | Label             | Reciprocal       | Level  |
| -------- | ------------------- | ----------------- | ---------------- | ------ |
| Decision | Spec                | `affects`         | `affected-by`    | SHOULD |
| Decision | Decision            | `supersedes`      | `superseded-by`  | MUST   |
| Plan     | Spec                | `implements`      | `implemented-by` | SHOULD |
| Plan     | Decision            | `implements`      | `implemented-by` | MAY    |
| Plan     | Decision            | `affected-by`     | `affects`        | MAY    |
| Evidence | Plan                | `validates`       | `validated-by`   | SHOULD |
| Evidence | Spec, Decision      | `supports`        | `supported-by`   | SHOULD |
| Evidence | actor               | `produced-by`     | none             | MUST   |
| Evidence | actor               | `reviewed-by`     | none             | MAY    |
| any item | child item          | `decomposes-into` | `part-of`        | SHOULD |
| any item | repository artifact | `anchored-to`     | none             | SHOULD |

A label outside the table (`unknown-label`) has to be mapped onto one that is in it, or dropped. A defined label used in a direction the table does not define (`direction-invalid`) usually means the two ends should be swapped.

`non-item-target` means a label that only connects blueprint items is pointing at something outside the store. Only `anchored-to`, `produced-by`, and `reviewed-by` may do that.

The reverse is `direction-invalid` too: `produced-by` and `reviewed-by` name the human, agent, CI system, or tool behind the Evidence, so a target that resolves to a blueprint item is the wrong direction. It is reported, never rewritten into a `blueprint:<id>` target.

`incomplete-relationship` means an entry is missing its `label` or its `target`. Nothing about it can be classified — not its direction, not its reciprocal — so it is reported and skipped. Supply the missing half from what the entry was for, or delete the entry.

Section 11.2 also narrows how a Plan reaches reasoning: a Plan should reach Decision reasoning through the Specs it implements, and relate directly to a Decision only in the two cases the table marks MAY.

## 10. Decisions state a Y-statement

preview.1 asked a Decision to explain an important choice and suggested context, decision, consequences, and alternatives as sections. preview.4 makes the Y-statement the required form:

> In the context of `<use case or user story>`, facing `<concern>`, we decided for `<option>` to achieve `<quality>`, accepting `<downside>`.

A long form adds the neglected options and rationale, and is used only when those carry real content:

> In the context of `<use case>`, facing `<concern>`, we decided for `<option>` and neglected `<other options>`, to achieve `<quality>`, accepting `<downside>`, because `<rationale>`.

Context, consequences, and alternatives may still appear below the statement, only when they hold something the statement cannot.

Build the statement from what the Decision already records. The concern and the downside are the two parts most often missing from a preview.1 Decision, and they are the two most tempting to invent. If the source does not say what was given up, ask — an invented downside is worse than an incomplete Decision, because it reads as history.

Alternatives sections must list only options that were genuinely evaluated. Delete a speculative list rather than keeping it.

## 11. Specs are present tense, not narratives

preview.1 said a Spec must describe what should be true now and must not be a history log. preview.4 adds the failure mode by name: a Spec must not be written as planned work or as a change narrative.

> - Spec: The API rejects a request without a valid token.
> - Not a Spec: We will add token validation, replacing the old cookie check.

Rewrite a narrative into the state it produced. The reasoning behind the change belongs in a Decision, and the execution belongs in a Plan.

preview.4 also changes what a Spec is obliged to contain: intent and current desired behavior are required; constraints, non-goals, anchors, and relationships are optional and included only when they carry real content. A preview.1 Spec padded with an empty "Non-goals" heading should have that heading deleted, not filled.

## 12. Evidence records an executed check and its producer

preview.4 states it directly: Evidence records the observed result of a check executed against the system. Authoring a document, creating a blueprint item, or restating that content exists is not a validation result.

The `produced-by` requirement is also enforced harder. Under preview.1 a failed check marked the Evidence invalid. Under preview.4 the validator must report the failure, and the Evidence must not be treated as supporting any claim until the relationship exists. Recording a producer anywhere other than a `produced-by` relationship does not satisfy it.

```diff
 relationships:
     - label: validates
       target: blueprint:plan.index-dotnet-code
+    - label: produced-by
+      target: urn:agent:example-agent
```

`validation logs` was dropped from the list of things Evidence may include; command output, test results, CI results, review results, benchmarks, and manual approval records remain.

A fenced block is not on its own an observed result — Evidence that pastes the command it ran and stops there still records no outcome. Write down what happened.

An Evidence item that records no executed check is not fixable by editing — either the check was run and the result was never written down, in which case recover it, or nothing was validated, in which case the item should not exist.

## 13. Body size budget

A blueprint item body SHOULD be at most 400 words. An item that outgrows it SHOULD be split into child items related with `decomposes-into` and `part-of`.

`scan.mjs` counts words outside fenced code blocks, so Evidence carrying command output is not flagged for the output alone.

Split along meaning, not length: each child owns one thing the parent was trying to say. A parent that becomes a stub with only links is a sign the split went one level too far.

## 14. Plans and Evidence only when meaningful

preview.1: "A Plan MUST exist for meaningful code-changing work unless the change is explicitly validated as having no blueprint impact."

preview.4: Specs are the backbone. A Plan is required only when execution spans multiple sessions or agents, or needs handoff, and MUST NOT be created solely to satisfy the framework. Evidence is required only when a validation step was executed and its result matters, and MUST NOT be created for work that was authored but not validated.

A preview.1 store built under the old rule often carries ceremonial Plans and Evidence created because the framework asked for them. The scripts do not detect these — nothing in the file distinguishes a thin Plan from a real one.

Review them by hand, and do not delete anything without asking. A ceremonial Plan that is already `completed` is often better left as it is: it is a record of what happened, and removing it costs history to buy tidiness.

## 15. Frontmatter and body hygiene

preview.4 adds two checks the Markdown implementation did not state before:

- Every YAML mapping in frontmatter must have unique field names. Duplicate keys, including a duplicate `id`, `type`, `status`, or `relationships`, are rejected **before** the metadata is accepted. Keep the intended value and delete the other; check the item's history if it is not obvious which one is intended.
- Every item's Markdown body must contain non-empty content.

`unsupported-frontmatter` means the frontmatter uses a YAML construct the scripts refuse to interpret — an alias, an anchor, a custom tag, a flow collection, or a block scalar. preview.4 discovery disables aliases and custom tags outright. Rewrite the construct as plain block mappings and sequences.

A file the scripts cannot read as a blueprint item at all is reported under the same change, and nothing else about it is scanned — so it keeps its old version stamp until the file itself is fixed:

| Code                       | Meaning                                                                                 |
| -------------------------- | --------------------------------------------------------------------------------------- |
| `missing-frontmatter`      | The file does not open with a `---` delimiter.                                          |
| `unterminated-frontmatter` | No closing `---` within the frontmatter byte limit.                                     |
| `invalid-utf8`             | The bytes are not valid UTF-8.                                                          |
| `file-too-large`           | The file is past the per-item byte limit. Split it, or confirm it belongs in the store. |
| `unreadable-file`          | The file could not be read — it vanished mid-scan, or its permissions deny it.          |
