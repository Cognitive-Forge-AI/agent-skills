#!/usr/bin/env node
// Apply the mechanical half of the 0.1.0-preview.1 -> 0.1.0-preview.4 upgrade.
//
//   node fix.mjs [--root .blueprints] [--write] [--only code,code] [--no-git]
//
// Dry run by default. Edits are applied line by line, never by re-serializing
// YAML, so comments, quoting, and indentation survive untouched.
//
// Anything this script refuses to rewrite is printed under "left for the
// agent" -- nothing is silently dropped.

import { writeFileSync } from 'node:fs';
import { analyze, applyEdits, TARGET_VERSION } from './lib/rules.mjs';
import { loadStore } from './lib/store.mjs';

/**
 * Read an option that takes a value, refusing a missing or empty one.
 *
 * Without this, `--root` at the end of the line reaches path.resolve as
 * undefined and throws, `--only` at the end of the line throws on .split,
 * `--root=` resolves to the working directory and rewrites the whole
 * repository, and `--root --write` silently targets a directory named
 * `--write`.
 */
function takeValue(options, name, raw) {
    if (raw === undefined || raw === '' || raw.startsWith('--')) {
        options.error = name + ' requires a value.';
        return null;
    }
    return raw;
}

function parseArgs(argv) {
    const options = { root: '.blueprints', write: false, git: true, only: null };
    const setRoot = (raw) => {
        const value = takeValue(options, '--root', raw);
        if (value !== null) options.root = value;
    };
    const setOnly = (raw) => {
        const value = takeValue(options, '--only', raw);
        if (value !== null) options.only = new Set(value.split(','));
    };

    for (let i = 0; i < argv.length; i += 1) {
        const arg = argv[i];
        if (arg === '--write') options.write = true;
        else if (arg === '--no-git') options.git = false;
        else if (arg === '--root') setRoot(argv[++i]);
        else if (arg.startsWith('--root=')) setRoot(arg.slice('--root='.length));
        else if (arg === '--only') setOnly(argv[++i]);
        else if (arg.startsWith('--only=')) setOnly(arg.slice('--only='.length));
        else if (arg === '--help' || arg === '-h') options.help = true;
        else if (!arg.startsWith('-')) options.root = arg;
        // A mistyped flag used to be dropped, so `--wrte` silently ran a dry
        // run and `--no-gti` silently kept git enabled.
        else options.error = 'Unrecognized option: ' + arg;
    }
    return options;
}

const HELP = [
    'Usage: node fix.mjs [--root <dir>] [--write] [--only <codes>] [--no-git]',
    '',
    '  --root <dir>    Blueprints store to upgrade. Default: .blueprints',
    '  --write         Apply the edits. Without it, nothing is written.',
    '  --only <codes>  Comma-separated finding codes to apply, e.g. version-stamp,plan-status-active',
    '  --no-git        Skip git lookups; anchor revisions are left for the agent.',
].join('\n');

const options = parseArgs(process.argv.slice(2));
if (options.help) {
    process.stdout.write(HELP + '\n');
    process.exit(0);
}
if (options.error) {
    process.stderr.write(options.error + '\n\n' + HELP + '\n');
    process.exit(2);
}

const store = loadStore(options.root);
if (store.missing) {
    process.stderr.write('No Blueprints store found at ' + store.root + '\n');
    process.exit(1);
}

const { findings } = analyze(store, { git: options.git });

const selected = findings.filter((f) => f.class === 'mechanical' && f.edit && (!options.only || options.only.has(f.code)));
const deferred = findings.filter((f) => f.class === 'needs-agent');
const skipped = findings.filter((f) => f.class === 'mechanical' && f.edit && options.only && !options.only.has(f.code));

const byItem = new Map();
for (const finding of selected) {
    const list = byItem.get(finding.item) || [];
    list.push(finding);
    byItem.set(finding.item, list);
}

const itemsByPath = new Map(store.items.map((item) => [item.relPath, item]));

const out = [];
out.push((options.write ? 'Applying' : 'Dry run') + ' -> ' + TARGET_VERSION);
out.push('Store: ' + store.root);
out.push('');

let edited = 0;

for (const [relPath, list] of [...byItem.entries()].sort()) {
    const item = itemsByPath.get(relPath);
    if (!item || !item.lines) continue;

    const text = applyEdits(item, list);

    out.push(relPath);
    for (const finding of [...list].sort((a, b) => a.edit.line - b.edit.line)) {
        out.push('  [' + finding.code + '] line ' + (finding.edit.line + 1) + ': ' + finding.message);
    }

    if (options.write) {
        writeFileSync(item.path, text, 'utf8');
    }
    edited += 1;
}

if (edited === 0) out.push('No mechanical edits to apply.');
out.push('');
out.push('Files ' + (options.write ? 'changed' : 'that would change') + ': ' + edited);
out.push('Mechanical edits: ' + selected.length);

if (skipped.length > 0) {
    out.push('');
    out.push('Skipped by --only: ' + skipped.length);
    for (const finding of skipped) out.push('  [' + finding.code + '] ' + finding.item);
}

if (deferred.length > 0) {
    out.push('');
    out.push('Left for the agent: ' + deferred.length);
    const seen = new Map();
    for (const finding of deferred) seen.set(finding.code, (seen.get(finding.code) || 0) + 1);
    for (const [code, count] of [...seen.entries()].sort((a, b) => b[1] - a[1])) {
        out.push('  [' + code + '] x' + count);
    }
    out.push('  Run scan.mjs for the full list.');
}

if (!options.write && selected.length > 0) {
    out.push('');
    out.push('Nothing was written. Re-run with --write to apply.');
}

process.stdout.write(out.join('\n') + '\n');
