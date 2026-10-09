// preview.1 -> preview.4 upgrade rules.
//
// One classification pass shared by scan.mjs and fix.mjs, so the report and
// the rewrite can never disagree about what a finding is.
//
// Every finding carries a `class`:
//   mechanical  - the rewrite is fully determined; `edit` describes it.
//   needs-agent - the fix requires reading the item's meaning, a git history
//                 choice, or a user answer. Never rewritten automatically.
//
// Catalog numbers in `change` match references/preview-1-to-preview-4.md.

import { execFileSync } from 'node:child_process';
import { ID_PATTERN, ITEM_TYPES, itemTypeOf, resolveTarget, splitComment } from './store.mjs';

export const TARGET_VERSION = '0.1.0-preview.4';
export const BODY_WORD_BUDGET = 400;

/** Reciprocal pairs from Core section 11.1. */
export const RECIPROCALS = {
    affects: 'affected-by',
    'affected-by': 'affects',
    supersedes: 'superseded-by',
    'superseded-by': 'supersedes',
    implements: 'implemented-by',
    'implemented-by': 'implements',
    validates: 'validated-by',
    'validated-by': 'validates',
    supports: 'supported-by',
    'supported-by': 'supports',
    'decomposes-into': 'part-of',
    'part-of': 'decomposes-into',
};

/** Labels whose target sits outside the blueprint system, so nothing records a reciprocal. */
export const NO_RECIPROCAL = new Set(['anchored-to', 'produced-by', 'reviewed-by']);

export const VALID_LABELS = new Set([...Object.keys(RECIPROCALS), ...NO_RECIPROCAL]);

/** Labels preview.1 defined that preview.4 no longer accepts in that direction. */
const ANY = '*';
const ALLOWED_DIRECTIONS = [
    ['decision', 'affects', 'spec'],
    ['decision', 'affects', 'plan'],
    ['spec', 'affected-by', 'decision'],
    ['plan', 'affected-by', 'decision'],
    ['decision', 'supersedes', 'decision'],
    ['decision', 'superseded-by', 'decision'],
    ['plan', 'implements', 'spec'],
    ['plan', 'implements', 'decision'],
    ['spec', 'implemented-by', 'plan'],
    ['decision', 'implemented-by', 'plan'],
    ['evidence', 'validates', 'plan'],
    ['plan', 'validated-by', 'evidence'],
    ['evidence', 'supports', 'spec'],
    ['evidence', 'supports', 'decision'],
    ['spec', 'supported-by', 'evidence'],
    ['decision', 'supported-by', 'evidence'],
    [ANY, 'decomposes-into', ANY],
    [ANY, 'part-of', ANY],
];

const FULL_OID = /^git:(?:[0-9a-f]{40}|[0-9a-f]{64})$/;
// Anything hex-shaped that is not already a prefixed full object id: an
// abbreviation, or a full SHA-1 or SHA-256 written without the `git:` prefix.
const BARE_HEX = /^(?:git:)?([0-9a-f]{4,64})$/;

function directionAllowed(fromType, label, toType) {
    return ALLOWED_DIRECTIONS.some(([f, l, t]) => l === label && (f === ANY || f === fromType) && (t === ANY || t === toType));
}

function fieldLine(entry, key) {
    return entry.fields[key] ? entry.fields[key].line : entry.startLine;
}

function fieldValue(entry, key) {
    return entry.fields[key] ? entry.fields[key].value : null;
}

/** A key written with nothing after it carries no more meaning than an absent one. */
function blank(value) {
    return value === null || value.trim() === '';
}

/** Rewrite one `key: value` line, preserving its leading text and trailing comment. */
function replaceValue(item, line, key, value) {
    const raw = item.lines[line];
    const idx = raw.indexOf(key + ':');
    const prefix = raw.slice(0, idx + key.length + 1);
    const { comment } = splitComment(raw.slice(idx + key.length + 1));
    return { kind: 'replace-line', line, text: prefix + ' ' + value + comment };
}

/** A replace must run before an insert at the same line. See `applyEdits`. */
const EDIT_ORDER = { 'replace-line': 0, 'insert-lines': 1 };

/**
 * Apply a file's mechanical edits and return the new text.
 *
 * Edits run in descending line order so an earlier one never shifts the line
 * numbers a later one was computed against.
 *
 * Two edits can land on the same line: a reciprocal is inserted at the line
 * after the last relationship entry, which is exactly where a `blueprints` or
 * `status` line sits when those keys follow the `relationships:` block. The
 * replacement has to run first, because the insertion pushes that line down and
 * a later replacement would overwrite the newly inserted text instead.
 */
export function applyEdits(item, findings) {
    const ordered = findings
        .filter((finding) => finding.edit)
        .slice()
        .sort((a, b) => b.edit.line - a.edit.line || EDIT_ORDER[a.edit.kind] - EDIT_ORDER[b.edit.kind]);

    const lines = [...item.lines];
    for (const { edit } of ordered) {
        if (edit.kind === 'replace-line') lines[edit.line] = edit.text;
        else if (edit.kind === 'insert-lines') lines.splice(edit.line, 0, ...edit.texts);
    }
    return lines.join(item.eol);
}

/** Locate the repository that owns the store, so `file:` anchors can be resolved. */
export function findRepoRoot(startDir) {
    try {
        const out = execFileSync('git', ['-C', startDir, 'rev-parse', '--show-toplevel'], {
            encoding: 'utf8',
            stdio: ['ignore', 'pipe', 'ignore'],
        });
        return out.trim();
    } catch {
        return null;
    }
}

function gitCommitOid(repoRoot, rev) {
    try {
        const out = execFileSync('git', ['-C', repoRoot, 'rev-parse', '--verify', '--end-of-options', rev + '^{commit}'], {
            encoding: 'utf8',
            stdio: ['ignore', 'pipe', 'ignore'],
        });
        const oid = out.trim();
        return /^(?:[0-9a-f]{40}|[0-9a-f]{64})$/.test(oid) ? oid : null;
    } catch {
        return null;
    }
}

/**
 * Is this revision the name of a ref rather than an object id?
 *
 * A branch or tag whose name happens to be all hex digits -- `abc123`, `cafe`,
 * `deadbeef` -- looks exactly like an abbreviated commit id. Expanding it would
 * pin the anchor to whatever that ref points at today, which is the mutable
 * revision preview.4 set out to forbid.
 */
function isRefName(repoRoot, rev) {
    return ['refs/heads/', 'refs/tags/', 'refs/remotes/'].some((namespace) => {
        try {
            execFileSync('git', ['-C', repoRoot, 'show-ref', '--verify', '--quiet', namespace + rev], {
                stdio: ['ignore', 'ignore', 'ignore'],
            });
            return true;
        } catch {
            return false;
        }
    });
}

function gitPathExists(repoRoot, oid, path) {
    const cleaned = path.replace(/\/+$/, '');
    if (cleaned === '') return true;
    try {
        execFileSync('git', ['-C', repoRoot, 'cat-file', '-e', oid + ':' + cleaned], {
            stdio: ['ignore', 'ignore', 'ignore'],
        });
        return true;
    } catch {
        return false;
    }
}

/** Reject an anchor path that is absolute, carries a URI component, or escapes the repository. */
function inspectAnchorPath(target) {
    if (/^file:\/\//.test(target)) return { ok: false, reason: 'the `file:` target has an authority component' };
    const path = target.startsWith('file:') ? target.slice('file:'.length) : target;
    if (path === '') return { ok: false, reason: 'the `file:` target has an empty path' };
    if (/[?#]/.test(path)) return { ok: false, reason: 'the `file:` target has a query or fragment' };
    if (path.startsWith('/') || /^[A-Za-z]:[\\/]/.test(path)) {
        return { ok: false, reason: 'the `file:` target is an absolute path' };
    }

    const segments = path.split('/');
    let depth = 0;
    for (const segment of segments) {
        if (segment === '' || segment === '.') continue;
        if (segment === '..') {
            depth -= 1;
            if (depth < 0) return { ok: false, reason: 'the `file:` target escapes the repository root' };
        } else depth += 1;
    }
    return { ok: true, path };
}

function countWords(body) {
    const withoutFences = body.replace(/```[\s\S]*?```/g, ' ');
    const tokens = withoutFences.replace(/[#>*_`|-]+/g, ' ').match(/\S+/g);
    return tokens ? tokens.length : 0;
}

const Y_STATEMENT = /in the context of[\s\S]{0,600}?we decided (?:for|to)\b/i;
const NARRATIVE_MARKERS = [
    /\bwe will\b/i,
    /\bwe (?:added|removed|replaced|changed|renamed|introduced|migrated)\b/i,
    /\bwill be (?:added|removed|replaced|changed)\b/i,
    /\b(?:previously|used to|no longer)\b/i,
    /^#{1,6}\s*(?:history|changelog|change log)\b/im,
];
const RESULT_MARKERS = /(?:passed|failed|observed|exit code|\bresult\b|\berror\b|\d+\s+(?:tests?|checks?|assertions?))/i;

/**
 * Classify one store against the preview.4 rules that differ from preview.1.
 *
 * `options.repoRoot` overrides repository detection. `options.git` can be set
 * to false to skip every git lookup, in which case anchor revisions that would
 * otherwise be expanded are reported as needs-agent instead.
 */
export function analyze(store, options = {}) {
    const findings = [];
    const repoRoot = options.repoRoot !== undefined ? options.repoRoot : findRepoRoot(store.root);
    const useGit = options.git !== false && repoRoot !== null;

    const add = (finding) => findings.push(finding);

    // ---- Structural readability ------------------------------------------------

    for (const item of store.items) {
        for (const error of item.errors) {
            add({
                code: error.code,
                class: 'needs-agent',
                change: 15,
                item: item.relPath,
                line: null,
                message: error.message,
            });
        }

        if (!item.hasFrontmatter) continue;

        for (const dup of item.duplicateKeys) {
            add({
                code: 'duplicate-key',
                class: 'needs-agent',
                change: 15,
                item: item.relPath,
                line: dup.line,
                message:
                    'Duplicate `' +
                    dup.key +
                    '` key in ' +
                    dup.scope +
                    '. preview.4 rejects duplicate mapping keys; keep the intended value and delete the other.',
            });
        }

        for (const un of item.unsupported) {
            add({
                code: 'unsupported-frontmatter',
                class: 'needs-agent',
                change: 15,
                item: item.relPath,
                line: un.line,
                message: 'Frontmatter uses ' + un.reason + ', which this tool will not rewrite. Resolve it by hand.',
            });
        }

        if (item.body.trim() === '') {
            add({
                code: 'empty-body',
                class: 'needs-agent',
                change: 15,
                item: item.relPath,
                line: null,
                message: 'preview.4 requires a non-empty Markdown body. Write the content or delete the item.',
            });
        }
    }

    // ---- Identity --------------------------------------------------------------

    for (const [id, items] of store.byId) {
        if (items.length > 1) {
            add({
                code: 'duplicate-id',
                class: 'needs-agent',
                change: 8,
                item: items.map((i) => i.relPath).join(', '),
                line: null,
                message: '`' + id + '` is used by ' + items.length + ' files. Item ids must be unique.',
            });
        }
    }

    for (const item of store.items) {
        if (!item.hasFrontmatter) continue;

        if (!item.id) {
            add({
                code: 'missing-id',
                class: 'needs-agent',
                change: 8,
                item: item.relPath,
                line: null,
                message: 'No `id` field. Required by Core section 5.',
            });
        } else if (!ID_PATTERN.test(item.id)) {
            add({
                code: 'invalid-id',
                class: 'needs-agent',
                change: 8,
                item: item.relPath,
                line: item.fields.id.line,
                message:
                    '`' +
                    item.id +
                    '` does not match the preview.4 identifier grammar (lowercase letters and digits, single internal hyphens, dot-separated segments). Renaming it also renames the file and every inbound `blueprint:` target.',
            });
        } else if (item.fileName !== item.id + '.md') {
            add({
                code: 'filename-mismatch',
                class: 'needs-agent',
                change: 8,
                item: item.relPath,
                line: null,
                message: 'File name does not match the item id. Expected `' + item.id + '.md`.',
            });
        }

        if (item.id && item.type && item.id.split('.')[0] !== item.type) {
            add({
                code: 'prefix-mismatch',
                class: 'needs-agent',
                change: 8,
                item: item.relPath,
                line: item.fields.id.line,
                message: 'Id prefix `' + item.id.split('.')[0] + '` does not match `type: ' + item.type + '`.',
            });
        }

        if (item.type && !ITEM_TYPES.includes(item.type)) {
            add({
                code: 'unknown-type',
                class: 'needs-agent',
                change: 8,
                item: item.relPath,
                line: item.fields.type.line,
                message: '`type: ' + item.type + '` is not one of spec, decision, plan, evidence.',
            });
        }
    }

    // ---- Change 1: framework version stamp -------------------------------------

    for (const item of store.items) {
        if (!item.hasFrontmatter) continue;
        const field = item.fields.blueprints;
        if (!field) {
            add({
                code: 'missing-blueprints-field',
                class: 'needs-agent',
                change: 1,
                item: item.relPath,
                line: null,
                message:
                    'No `blueprints` field. It was required in preview.1 too, so confirm the item really belongs to this store before stamping ' +
                    TARGET_VERSION +
                    '.',
            });
        } else if (field.value !== TARGET_VERSION) {
            add({
                code: 'version-stamp',
                class: 'mechanical',
                change: 1,
                item: item.relPath,
                line: field.line,
                message: 'blueprints: ' + field.value + ' -> ' + TARGET_VERSION,
                edit: replaceValue(item, field.line, 'blueprints', TARGET_VERSION),
            });
        }
    }

    // ---- Change 2: PlanStatus `active` was removed ------------------------------

    for (const item of store.items) {
        if (itemTypeOf(item) !== 'plan' || item.status !== 'active') continue;
        add({
            code: 'plan-status-active',
            class: 'mechanical',
            change: 2,
            item: item.relPath,
            line: item.fields.status.line,
            message: 'status: active -> in-progress (preview.4 replaced the Plan `active` state)',
            edit: replaceValue(item, item.fields.status.line, 'status', 'in-progress'),
        });
    }

    // ---- Relationships ---------------------------------------------------------

    const reciprocalWanted = []; // { targetItem, label, sourceId, sourceRelPath }

    for (const item of store.items) {
        if (!item.hasFrontmatter) continue;
        const fromType = itemTypeOf(item);

        for (const rel of item.relationships) {
            const label = fieldValue(rel, 'label');
            const target = fieldValue(rel, 'target');
            const labelLine = fieldLine(rel, 'label');
            const targetLine = fieldLine(rel, 'target');

            if (blank(label) || blank(target)) {
                add({
                    code: 'incomplete-relationship',
                    class: 'needs-agent',
                    change: 9,
                    item: item.relPath,
                    line: rel.startLine,
                    message: 'Relationship entry is missing `label` or `target`.',
                });
                continue;
            }

            // -- Change 5 and 6: anchored-to target and revision ------------------
            if (label === 'anchored-to') {
                const inspected = inspectAnchorPath(target);
                if (!inspected.ok) {
                    add({
                        code: 'anchor-target-invalid',
                        class: 'needs-agent',
                        change: 6,
                        item: item.relPath,
                        line: targetLine,
                        message: 'preview.4 requires a repository-relative `file:<path>` anchor, but ' + inspected.reason + '.',
                    });
                    continue;
                }

                if (!target.startsWith('file:')) {
                    add({
                        code: 'anchor-target-scheme',
                        class: 'mechanical',
                        change: 6,
                        item: item.relPath,
                        line: targetLine,
                        message: 'target: ' + target + ' -> file:' + target,
                        edit: replaceValue(item, targetLine, 'target', 'file:' + target),
                    });
                }

                const revisionField = rel.fields.revision;
                if (!revisionField) {
                    add({
                        code: 'anchor-revision-missing',
                        class: 'needs-agent',
                        change: 5,
                        item: item.relPath,
                        line: rel.startLine,
                        message:
                            '`revision` is REQUIRED for `anchored-to` in preview.4. Choose the commit the anchor should pin to and record it as `git:<full-commit-object-id>`.',
                    });
                    continue;
                }

                const revision = revisionField.value;
                if (FULL_OID.test(revision)) {
                    if (useGit) {
                        const oid = revision.slice('git:'.length);
                        if (!gitCommitOid(repoRoot, oid)) {
                            add({
                                code: 'anchor-revision-unresolvable',
                                class: 'needs-agent',
                                change: 5,
                                item: item.relPath,
                                line: revisionField.line,
                                message: '`' + revision + '` does not resolve to a commit in this repository.',
                            });
                        } else if (!gitPathExists(repoRoot, oid, inspected.path)) {
                            add({
                                code: 'anchor-path-missing-at-revision',
                                class: 'needs-agent',
                                change: 5,
                                item: item.relPath,
                                line: revisionField.line,
                                message: '`' + inspected.path + '` does not exist in the tree of ' + revision + '.',
                            });
                        }
                    }
                    continue;
                }

                const hex = BARE_HEX.exec(revision);
                if (!hex || (useGit && isRefName(repoRoot, hex[1]))) {
                    add({
                        code: 'anchor-revision-symbolic',
                        class: 'needs-agent',
                        change: 5,
                        item: item.relPath,
                        line: revisionField.line,
                        message:
                            '`' +
                            revision +
                            '` is not an immutable revision. Branch names, tags, and symbolic refs are no longer accepted; record the full commit object id the anchor means.',
                    });
                    continue;
                }

                if (!useGit) {
                    add({
                        code: 'anchor-revision-abbreviated',
                        class: 'needs-agent',
                        change: 5,
                        item: item.relPath,
                        line: revisionField.line,
                        message:
                            '`' + revision + '` is not in `git:<full-object-id>` form and no git repository was available to expand it.',
                    });
                    continue;
                }

                const oid = gitCommitOid(repoRoot, hex[1]);
                if (!oid) {
                    add({
                        code: 'anchor-revision-unresolvable',
                        class: 'needs-agent',
                        change: 5,
                        item: item.relPath,
                        line: revisionField.line,
                        message:
                            '`' +
                            revision +
                            '` does not resolve to a commit in this repository. It may be ambiguous, from another repository, or unreachable.',
                    });
                    continue;
                }
                if (!gitPathExists(repoRoot, oid, inspected.path)) {
                    add({
                        code: 'anchor-path-missing-at-revision',
                        class: 'needs-agent',
                        change: 5,
                        item: item.relPath,
                        line: revisionField.line,
                        message:
                            '`' +
                            revision +
                            '` expands to ' +
                            oid +
                            ', but `' +
                            inspected.path +
                            '` does not exist in that commit. Pick the commit the anchor actually means.',
                    });
                    continue;
                }

                add({
                    code: 'anchor-revision-expand',
                    class: 'mechanical',
                    change: 5,
                    item: item.relPath,
                    line: revisionField.line,
                    message: 'revision: ' + revision + ' -> git:' + oid,
                    edit: replaceValue(item, revisionField.line, 'revision', 'git:' + oid),
                });
                continue;
            }

            // -- Change 3: canonical blueprint: targets ---------------------------
            const resolved = resolveTarget(target, store);

            // `produced-by` and `reviewed-by` name an actor, so a target outside
            // the store is exactly what they are for. Every other label connects
            // two blueprint items.
            if (resolved.kind === 'external' && !NO_RECIPROCAL.has(label)) {
                add({
                    code: 'non-item-target',
                    class: 'needs-agent',
                    change: 9,
                    item: item.relPath,
                    line: targetLine,
                    message:
                        'Label `' +
                        label +
                        '` points at `' +
                        target +
                        '`, which is not a blueprint item. Only `anchored-to`, `produced-by`, and `reviewed-by` may target something outside the store.',
                });
                continue;
            }

            // The same two labels name an *actor*, so a target that resolves to
            // a blueprint item is the wrong direction. Caught before the
            // canonicalization below, which would otherwise rewrite a path
            // target into `blueprint:<id>` and make the wrong relationship look
            // like a well-formed one.
            if ((label === 'produced-by' || label === 'reviewed-by') && (resolved.kind === 'canonical' || resolved.kind === 'path')) {
                add({
                    code: 'direction-invalid',
                    class: 'needs-agent',
                    change: 9,
                    item: item.relPath,
                    line: labelLine,
                    message:
                        '`' +
                        label +
                        '` names the human, agent, CI system, or tool behind the Evidence, but `' +
                        target +
                        '` is a blueprint item. Point it at the actor instead.',
                });
                continue;
            }

            // An id carried by more than one file cannot be resolved to the
            // item the relationship means, so it is not rewritten either.
            if (resolved.kind === 'canonical' && resolved.ambiguous) {
                add({
                    code: 'unresolvable-target',
                    class: 'needs-agent',
                    change: 3,
                    item: item.relPath,
                    line: targetLine,
                    message: '`' + target + '` names an id used by more than one file, so it does not identify a single item.',
                });
                continue;
            }

            if (resolved.kind === 'path') {
                if (resolved.ambiguous || !resolved.item || !resolved.item.id) {
                    add({
                        code: 'unresolvable-target',
                        class: 'needs-agent',
                        change: 3,
                        item: item.relPath,
                        line: targetLine,
                        message:
                            '`' +
                            target +
                            '` ' +
                            (resolved.ambiguous ? 'matches more than one item file' : 'does not match any item in the store') +
                            ', so it cannot be rewritten to a canonical `blueprint:<id>` target automatically.',
                    });
                    continue;
                }
                add({
                    code: 'target-to-canonical',
                    class: 'mechanical',
                    change: 3,
                    item: item.relPath,
                    line: targetLine,
                    message: 'target: ' + target + ' -> blueprint:' + resolved.item.id,
                    edit: replaceValue(item, targetLine, 'target', 'blueprint:' + resolved.item.id),
                });
            } else if (resolved.kind === 'canonical' && resolved.trailingExtension) {
                add({
                    code: 'target-trailing-extension',
                    class: 'mechanical',
                    change: 3,
                    item: item.relPath,
                    line: targetLine,
                    message: 'target: ' + target + ' -> blueprint:' + resolved.id,
                    edit: replaceValue(item, targetLine, 'target', 'blueprint:' + resolved.id),
                });
            } else if (resolved.kind === 'canonical' && !resolved.item) {
                add({
                    code: 'unresolvable-target',
                    class: 'needs-agent',
                    change: 3,
                    item: item.relPath,
                    line: targetLine,
                    message: '`' + target + '` does not resolve to any item id in the store.',
                });
            }

            const targetItem = resolved.item;
            const toType = targetItem ? itemTypeOf(targetItem) : null;

            // -- Change 4: `supported-by` changed meaning -------------------------
            let effectiveLabel = label;
            if (label === 'supported-by') {
                if (toType === 'decision') {
                    add({
                        code: 'supported-by-remap',
                        class: 'mechanical',
                        change: 4,
                        item: item.relPath,
                        line: labelLine,
                        message:
                            'label: supported-by -> affected-by (preview.4 reassigned `supported-by` to Evidence; a Decision now `affects` a Spec)',
                        edit: replaceValue(item, labelLine, 'label', 'affected-by'),
                    });
                    effectiveLabel = 'affected-by';
                } else if (toType !== 'evidence') {
                    add({
                        code: 'supported-by-ambiguous',
                        class: 'needs-agent',
                        change: 4,
                        item: item.relPath,
                        line: labelLine,
                        message:
                            '`supported-by` means something different in preview.4 (the reciprocal of Evidence `supports`) and this target is ' +
                            (toType ? 'a ' + toType : 'unresolved') +
                            '. Reclassify the relationship by hand.',
                    });
                    continue;
                }
            }

            // -- Change 9: labels outside the preview.4 table ---------------------
            if (!VALID_LABELS.has(effectiveLabel)) {
                add({
                    code: 'unknown-label',
                    class: 'needs-agent',
                    change: 9,
                    item: item.relPath,
                    line: labelLine,
                    message:
                        '`' +
                        effectiveLabel +
                        '` is not a label in the preview.4 relationship reference. Custom labels are not allowed; map it onto a defined label or drop it.',
                });
                continue;
            }

            if (NO_RECIPROCAL.has(effectiveLabel)) {
                if (effectiveLabel !== 'anchored-to' && fromType !== 'evidence') {
                    add({
                        code: 'direction-invalid',
                        class: 'needs-agent',
                        change: 9,
                        item: item.relPath,
                        line: labelLine,
                        message: '`' + effectiveLabel + '` is defined only from an Evidence item, not from a ' + fromType + '.',
                    });
                }
                continue;
            }

            if (fromType && toType && !directionAllowed(fromType, effectiveLabel, toType)) {
                add({
                    code: 'direction-invalid',
                    class: 'needs-agent',
                    change: 9,
                    item: item.relPath,
                    line: labelLine,
                    message:
                        'preview.4 does not define `' +
                        effectiveLabel +
                        '` from a ' +
                        fromType +
                        ' to a ' +
                        toType +
                        '. Check the relationship reference and use the direction it defines.',
                });
                continue;
            }

            // -- Change 7: record the reciprocal ----------------------------------
            // Writing `blueprint:<id>` into another file only helps when that
            // id is one this store can resolve: an invalid id is about to be
            // renamed, and a duplicated one names no single item.
            const sourceIdUsable = Boolean(item.id) && ID_PATTERN.test(item.id || '') && (store.byId.get(item.id) || []).length === 1;
            if (targetItem && sourceIdUsable) {
                reciprocalWanted.push({
                    targetItem,
                    label: RECIPROCALS[effectiveLabel],
                    sourceId: item.id,
                    sourceRelPath: item.relPath,
                });
            }
        }
    }

    // ---- Change 7: insert missing reciprocals ----------------------------------

    const pending = new Map(); // relPath -> [{label, targetId}]

    for (const want of reciprocalWanted) {
        const target = want.targetItem;
        if (!target.hasFrontmatter) continue;

        const already = target.relationships.some((rel) => {
            const label = fieldValue(rel, 'label');
            const value = fieldValue(rel, 'target');
            if (label !== want.label || value === null) return false;
            const resolved = resolveTarget(value, store);
            return resolved.id === want.sourceId;
        });
        if (already) continue;

        const queued = pending.get(target.relPath) || [];
        if (queued.some((q) => q.label === want.label && q.targetId === want.sourceId)) continue;
        queued.push({ label: want.label, targetId: want.sourceId, item: target, from: want.sourceRelPath });
        pending.set(target.relPath, queued);
    }

    for (const [relPath, queued] of pending) {
        const target = queued[0].item;
        const layout = relationshipLayout(target);
        add({
            code: 'missing-reciprocal',
            class: 'mechanical',
            change: 7,
            item: relPath,
            line: layout.insertAt,
            message: queued.map((q) => 'add `' + q.label + ' -> blueprint:' + q.targetId + '` (recorded from ' + q.from + ')').join('; '),
            edit: {
                kind: 'insert-lines',
                line: layout.insertAt,
                texts: layout.render(queued),
            },
        });
    }

    // ---- Change 12: Evidence must record its producer ---------------------------

    for (const item of store.items) {
        if (itemTypeOf(item) !== 'evidence' || !item.hasFrontmatter) continue;
        const hasProducer = item.relationships.some((rel) => fieldValue(rel, 'label') === 'produced-by');
        if (!hasProducer) {
            add({
                code: 'evidence-missing-produced-by',
                class: 'needs-agent',
                change: 12,
                item: item.relPath,
                line: null,
                message:
                    'Evidence has no `produced-by` relationship. preview.4 will not treat it as supporting any claim until one exists. Record the actor that produced it, for example `urn:agent:<name>`.',
            });
        }
        // A fenced block used to exempt the item outright, which passed any
        // Evidence that pasted the command it ran and nothing else.
        if (!RESULT_MARKERS.test(item.body)) {
            add({
                code: 'evidence-no-observed-result',
                class: 'needs-agent',
                change: 12,
                item: item.relPath,
                line: null,
                message:
                    'No observed result found in the body. preview.4 Evidence records what was executed and what happened; authoring a document is not a validation result.',
            });
        }
    }

    // ---- Changes 10, 11, 13: body shape ----------------------------------------

    for (const item of store.items) {
        if (!item.hasFrontmatter || item.body.trim() === '') continue;
        const type = itemTypeOf(item);

        const words = countWords(item.body);
        if (words > BODY_WORD_BUDGET) {
            add({
                code: 'body-over-budget',
                class: 'needs-agent',
                change: 13,
                item: item.relPath,
                line: null,
                message:
                    'Body is about ' +
                    words +
                    ' words, over the ' +
                    BODY_WORD_BUDGET +
                    '-word budget. Split it into child items related with `decomposes-into`.',
            });
        }

        if (type === 'decision' && !Y_STATEMENT.test(item.body)) {
            add({
                code: 'decision-missing-y-statement',
                class: 'needs-agent',
                change: 10,
                item: item.relPath,
                line: null,
                message:
                    'No Y-statement found. preview.4 requires: In the context of <use case>, facing <concern>, we decided for <option> to achieve <quality>, accepting <downside>. Build it from what the Decision already says; do not invent a concern or a downside.',
            });
        }

        if (type === 'spec') {
            const marker = NARRATIVE_MARKERS.find((re) => re.test(item.body));
            if (marker) {
                add({
                    code: 'spec-reads-as-narrative',
                    class: 'needs-agent',
                    change: 11,
                    item: item.relPath,
                    line: null,
                    message:
                        'Body contains change-narrative or history wording. preview.4 Specs describe the state expected to be true now. Rewrite the narrative into the state it produced, and move the reasoning to a Decision.',
                });
            }
        }
    }

    return { findings, repoRoot, gitAvailable: useGit };
}

/**
 * Work out where new relationship entries go in an item, and how to render them
 * so they match the indentation already used in that file.
 */
export function relationshipLayout(item) {
    const existing = item.relationships;
    const header = item.fields.relationships;

    if (existing.length > 0) {
        const last = existing[existing.length - 1];
        const dashIndent = ' '.repeat(last.dashIndent);
        const keyIndent = ' '.repeat(last.keyIndent);
        return {
            insertAt: last.endLine + 1,
            render: (entries) =>
                entries.flatMap((e) => [dashIndent + '- label: ' + e.label, keyIndent + 'target: blueprint:' + e.targetId]),
        };
    }

    const dashIndent = '    ';
    const keyIndent = '      ';
    const renderEntries = (entries) =>
        entries.flatMap((e) => [dashIndent + '- label: ' + e.label, keyIndent + 'target: blueprint:' + e.targetId]);

    if (header) {
        return { insertAt: header.line + 1, render: renderEntries };
    }

    return {
        insertAt: item.fmEnd,
        render: (entries) => ['relationships:'].concat(renderEntries(entries)),
    };
}
