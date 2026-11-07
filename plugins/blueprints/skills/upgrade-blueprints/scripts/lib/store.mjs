// Shared loader for a Markdown-based Blueprints store.
//
// Zero dependencies. Parses only the frontmatter shape the Markdown-Based
// Implementation specification defines: top-level scalars plus `sources` and
// `relationships` as block sequences of maps. Anything richer is reported
// rather than guessed at.
//
// Every field records the line it was read from, so fix.mjs can rewrite by
// line instead of re-serializing YAML and destroying the author's formatting.

import { closeSync, fstatSync, openSync, readSync, readdirSync, statSync } from 'node:fs';
import { join, relative, resolve, sep } from 'node:path';

export const MAX_FRONTMATTER_BYTES = 65536;
export const MAX_FILE_BYTES = 1048576;
export const MAX_FILES = 5000;
export const MAX_DEPTH = 10;

export const ITEM_TYPES = ['spec', 'decision', 'plan', 'evidence'];

/** Identifier grammar from Core section 5.1. */
export const ID_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*(?:\.[a-z0-9]+(?:-[a-z0-9]+)*)*$/;

export function toPosix(p) {
    return p.split(sep).join('/');
}

/** Collect every `*.md` file under `root`, bounded the way discovery is bounded. */
export function findItemFiles(root) {
    const files = [];
    const truncated = { files: false, depth: false };

    const walk = (dir, depth) => {
        if (depth > MAX_DEPTH) {
            truncated.depth = true;
            return;
        }
        let entries;
        try {
            entries = readdirSync(dir, { withFileTypes: true });
        } catch {
            return;
        }
        entries.sort((a, b) => (a.name < b.name ? -1 : a.name > b.name ? 1 : 0));
        for (const entry of entries) {
            if (files.length >= MAX_FILES) {
                truncated.files = true;
                return;
            }
            const full = join(dir, entry.name);
            if (entry.isSymbolicLink()) continue;
            if (entry.isDirectory()) walk(full, depth + 1);
            else if (entry.isFile() && entry.name.endsWith('.md')) files.push(full);
        }
    };

    walk(root, 0);
    return { files, truncated };
}

/**
 * Split an unquoted trailing comment off a value. A `#` inside quotes or in a
 * URI fragment is part of the value, not a comment.
 *
 * Returns `{ value, comment }`, where `comment` keeps the whitespace that
 * preceded it so a rewritten line can put it back exactly as the author left it.
 */
export function splitComment(raw) {
    let quote = null;
    for (let i = 0; i < raw.length; i += 1) {
        const ch = raw[i];
        if (quote) {
            if (ch === quote) quote = null;
        } else if (ch === '"' || ch === "'") {
            quote = ch;
        } else if (ch === '#' && i > 0 && /\s/.test(raw[i - 1])) {
            const value = raw.slice(0, i);
            return { value, comment: raw.slice(value.trimEnd().length) };
        }
    }
    return { value: raw, comment: '' };
}

function stripComment(value) {
    return splitComment(value).value;
}

function unquote(value) {
    const trimmed = value.trim();
    if (trimmed.length >= 2) {
        const first = trimmed[0];
        const last = trimmed[trimmed.length - 1];
        if ((first === '"' && last === '"') || (first === "'" && last === "'")) {
            return trimmed.slice(1, -1);
        }
    }
    return trimmed;
}

function scalar(raw) {
    return unquote(stripComment(raw));
}

const KEY_LINE = /^(\s*)([A-Za-z0-9_-]+):\s?(.*)$/;
const SEQ_LINE = /^(\s*)-\s+([A-Za-z0-9_-]+):\s?(.*)$/;
const SEQ_BARE = /^(\s*)-\s*$/;

/**
 * Read the restricted frontmatter subset.
 *
 * Returns `{ fields, sources, relationships, duplicateKeys, unsupported }`.
 * `unsupported` collects constructs this reader refuses to interpret (aliases,
 * anchors, custom tags, flow collections, block scalars) so callers can defer
 * to a human instead of acting on a partial reading.
 */
export function parseFrontmatter(lines, fmStart, fmEnd) {
    const fields = {};
    const sources = [];
    const relationships = [];
    const duplicateKeys = [];
    const unsupported = [];

    let current = null; // 'sources' | 'relationships' | null
    let entry = null;

    const closeEntry = () => {
        if (!entry) return;
        if (entry.list === 'sources') sources.push(entry);
        else relationships.push(entry);
        entry = null;
    };

    for (let i = fmStart + 1; i < fmEnd; i += 1) {
        const line = lines[i];
        if (line.trim() === '' || line.trim().startsWith('#')) continue;

        if (/:\s*[*&]\S/.test(line) || /^\s*-\s+[*&]\S/.test(line) || /:\s*!!?\S/.test(line)) {
            unsupported.push({ line: i, reason: 'YAML alias, anchor, or custom tag' });
            continue;
        }
        if (/^\s*(?:-\s+)?[A-Za-z0-9_-]+:\s*[[{]/.test(line)) {
            unsupported.push({ line: i, reason: 'flow collection' });
            continue;
        }
        if (/^\s*(?:-\s+)?[A-Za-z0-9_-]+:\s*[|>]\s*$/.test(line)) {
            unsupported.push({ line: i, reason: 'block scalar' });
            continue;
        }

        const seq = SEQ_LINE.exec(line);
        if (seq && current) {
            closeEntry();
            const [, indent, key, rawValue] = seq;
            entry = {
                list: current,
                startLine: i,
                endLine: i,
                dashIndent: indent.length,
                keyIndent: indent.length + 2,
                fields: { [key]: { value: scalar(rawValue), line: i, raw: rawValue } },
            };
            continue;
        }

        if (SEQ_BARE.test(line) && current) {
            closeEntry();
            const indent = /^(\s*)/.exec(line)[1].length;
            entry = { list: current, startLine: i, endLine: i, dashIndent: indent, keyIndent: indent + 2, fields: {} };
            continue;
        }

        const kv = KEY_LINE.exec(line);
        if (!kv) {
            unsupported.push({ line: i, reason: 'unrecognized frontmatter line' });
            continue;
        }

        const [, indent, key, rawValue] = kv;

        if (entry && indent.length >= entry.keyIndent) {
            if (Object.prototype.hasOwnProperty.call(entry.fields, key)) {
                duplicateKeys.push({ key, line: i, scope: entry.list + ' entry' });
            }
            entry.fields[key] = { value: scalar(rawValue), line: i, raw: rawValue };
            entry.endLine = i;
            continue;
        }

        closeEntry();

        if (indent.length === 0 && (key === 'sources' || key === 'relationships') && scalar(rawValue) === '') {
            if (Object.prototype.hasOwnProperty.call(fields, key)) {
                duplicateKeys.push({ key, line: i, scope: 'frontmatter' });
            }
            current = key;
            fields[key] = { value: '', line: i, raw: rawValue };
            continue;
        }

        if (indent.length === 0) {
            current = null;
            if (Object.prototype.hasOwnProperty.call(fields, key)) {
                duplicateKeys.push({ key, line: i, scope: 'frontmatter' });
            }
            fields[key] = { value: scalar(rawValue), line: i, raw: rawValue };
            continue;
        }

        unsupported.push({ line: i, reason: 'unexpected nesting' });
    }

    closeEntry();

    const shape = (list) =>
        list.map((e) => ({
            startLine: e.startLine,
            endLine: e.endLine,
            dashIndent: e.dashIndent,
            keyIndent: e.keyIndent,
            fields: e.fields,
        }));

    return { fields, sources: shape(sources), relationships: shape(relationships), duplicateKeys, unsupported };
}

/** Read one item file. Never throws; an unreadable or malformed file is reported instead. */
export function loadItem(path, root) {
    const item = {
        path,
        relPath: toPosix(relative(root, path)),
        fileName: toPosix(path).split('/').pop(),
        errors: [],
        fields: {},
        sources: [],
        relationships: [],
        duplicateKeys: [],
        unsupported: [],
        hasFrontmatter: false,
        body: '',
    };

    // A file can disappear, become unreadable, or be swapped for something
    // else between discovery and this read. Measure and read one descriptor so
    // the size limit applies to the bytes actually taken, and never read past
    // the limit even if the file grows underneath. Either way the item is
    // reported, never guessed at, and the scan survives.
    let buffer;
    let fd;
    try {
        fd = openSync(path, 'r');
        const stats = fstatSync(fd);
        if (!stats.isFile()) throw new Error('not a regular file');
        if (stats.size > MAX_FILE_BYTES) {
            item.errors.push({
                code: 'file-too-large',
                message: 'File is ' + stats.size + ' bytes, over the ' + MAX_FILE_BYTES + '-byte item limit.',
            });
            return item;
        }
        const bounded = Buffer.alloc(Math.min(stats.size, MAX_FILE_BYTES));
        let read = 0;
        while (read < bounded.length) {
            const n = readSync(fd, bounded, read, bounded.length - read, read);
            if (n === 0) break;
            read += n;
        }
        buffer = read === bounded.length ? bounded : bounded.subarray(0, read);
    } catch {
        item.errors.push({ code: 'unreadable-file', message: 'File could not be read.' });
        return item;
    } finally {
        if (fd !== undefined) {
            try {
                closeSync(fd);
            } catch {
                /* the descriptor is going away with the process anyway */
            }
        }
    }

    const text = buffer.toString('utf8');
    if (Buffer.compare(Buffer.from(text, 'utf8'), buffer) !== 0) {
        item.errors.push({ code: 'invalid-utf8', message: 'File is not valid UTF-8.' });
        return item;
    }

    item.eol = text.includes('\r\n') ? '\r\n' : '\n';
    const lines = text.split(/\r?\n/);
    item.lines = lines;
    item.raw = text;

    if (lines[0] === undefined || lines[0].trim() !== '---') {
        item.errors.push({ code: 'missing-frontmatter', message: 'File does not open with a `---` delimiter.' });
        item.body = text;
        return item;
    }

    let fmEnd = -1;
    let bytes = 0;
    for (let i = 1; i < lines.length; i += 1) {
        bytes += Buffer.byteLength(lines[i], 'utf8') + item.eol.length;
        if (bytes > MAX_FRONTMATTER_BYTES) break;
        if (lines[i].trim() === '---') {
            fmEnd = i;
            break;
        }
    }

    if (fmEnd === -1) {
        item.errors.push({
            code: 'unterminated-frontmatter',
            message: 'No closing `---` delimiter within ' + MAX_FRONTMATTER_BYTES + ' bytes.',
        });
        return item;
    }

    item.hasFrontmatter = true;
    item.fmStart = 0;
    item.fmEnd = fmEnd;
    item.body = lines.slice(fmEnd + 1).join('\n');

    const parsed = parseFrontmatter(lines, 0, fmEnd);
    Object.assign(item, parsed);

    item.id = parsed.fields.id ? parsed.fields.id.value : null;
    item.type = parsed.fields.type ? parsed.fields.type.value : null;
    item.status = parsed.fields.status ? parsed.fields.status.value : null;
    item.blueprints = parsed.fields.blueprints ? parsed.fields.blueprints.value : null;

    return item;
}

/** Load every item under `root` and index it by id and by file basename. */
export function loadStore(root) {
    const resolved = resolve(root);
    const empty = { root: resolved, missing: true, items: [], byId: new Map(), byBasename: new Map(), truncated: {} };

    let stats;
    try {
        stats = statSync(resolved);
    } catch {
        return empty;
    }
    if (!stats.isDirectory()) return empty;

    const { files, truncated } = findItemFiles(resolved);
    const items = files.map((file) => loadItem(file, resolved));

    const byId = new Map();
    const byBasename = new Map();
    for (const item of items) {
        if (item.id) {
            if (byId.has(item.id)) byId.get(item.id).push(item);
            else byId.set(item.id, [item]);
        }
        const base = item.fileName.replace(/\.md$/, '');
        if (byBasename.has(base)) byBasename.get(base).push(item);
        else byBasename.set(base, [item]);
    }

    return { root: resolved, missing: false, items, byId, byBasename, truncated };
}

/**
 * Resolve a relationship target that may name a blueprint item.
 *
 * Returns `{ kind, id, item, ambiguous }`. `kind` is one of `canonical`,
 * `path`, `external`, or `unknown`. Path targets resolve through the file
 * basename, which is the only link a preview.1 store has between a storage
 * path and a stable item id.
 */
export function resolveTarget(target, store) {
    const value = (target === undefined || target === null ? '' : target).trim();
    if (value === '') return { kind: 'unknown', id: null, item: null, ambiguous: false };

    if (value.startsWith('blueprint:')) {
        const id = value.slice('blueprint:'.length).replace(/\.md$/, '');
        const matches = store.byId.get(id) || [];
        return {
            kind: 'canonical',
            id,
            item: matches[0] || null,
            ambiguous: matches.length > 1,
            trailingExtension: value.endsWith('.md'),
        };
    }

    if (/^[a-z][a-z0-9+.-]*:/i.test(value) && !value.startsWith('file:')) {
        return { kind: 'external', id: null, item: null, ambiguous: false };
    }

    const withoutScheme = value.startsWith('file:') ? value.slice('file:'.length) : value;
    if (!withoutScheme.endsWith('.md')) {
        return { kind: 'external', id: null, item: null, ambiguous: false };
    }

    const base = withoutScheme.split(/[?#]/)[0].split('/').pop().replace(/\.md$/, '');
    const matches = store.byBasename.get(base) || [];
    return {
        kind: 'path',
        id: matches.length === 1 ? matches[0].id : base,
        item: matches.length === 1 ? matches[0] : null,
        ambiguous: matches.length > 1,
    };
}

export function itemTypeOf(item) {
    if (item && item.type) return item.type;
    const prefix = item && item.id ? item.id.split('.')[0] : null;
    return ITEM_TYPES.includes(prefix) ? prefix : null;
}
