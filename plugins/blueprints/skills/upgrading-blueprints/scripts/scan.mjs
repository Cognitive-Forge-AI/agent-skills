#!/usr/bin/env node
// Report what a Blueprints store needs in order to move from 0.1.0-preview.1
// to 0.1.0-preview.4. Read-only: this script never writes a file.
//
//   node scan.mjs [--root .blueprints] [--json] [--no-git]
//
// Findings are split into `mechanical` (fix.mjs rewrites them) and
// `needs-agent` (they require reading the item's meaning or a user answer).
// A findings exit code is always 0 -- this reports an upgrade delta, it does
// not gate. A usage error still exits non-zero.

import { relative } from 'node:path';
import { fileURLToPath } from 'node:url';

import { analyze, TARGET_VERSION } from './lib/rules.mjs';
import { loadStore, toPosix } from './lib/store.mjs';

/**
 * Read an option that takes a value, refusing a missing or empty one.
 *
 * Without this, `--root` at the end of the line reaches path.resolve as
 * undefined and throws, `--root=` resolves to the working directory and scans
 * the whole repository, and `--root --json` silently scans `--json`.
 */
function takeValue(options, name, raw) {
    if (raw === undefined || raw === '' || raw.startsWith('--')) {
        options.error = name + ' requires a value.';
        return null;
    }
    return raw;
}

function parseArgs(argv) {
    const options = { root: '.blueprints', json: false, git: true };
    const setRoot = (raw) => {
        const value = takeValue(options, '--root', raw);
        if (value !== null) options.root = value;
    };

    for (let i = 0; i < argv.length; i += 1) {
        const arg = argv[i];
        if (arg === '--json') options.json = true;
        else if (arg === '--no-git') options.git = false;
        else if (arg === '--root') setRoot(argv[++i]);
        else if (arg.startsWith('--root=')) setRoot(arg.slice('--root='.length));
        else if (arg === '--help' || arg === '-h') options.help = true;
        else if (!arg.startsWith('-')) options.root = arg;
        // A mistyped flag used to be dropped, so `--jsno` silently printed the
        // human summary and `--no-gti` silently kept git enabled.
        else options.error = 'Unrecognized option: ' + arg;
    }
    return options;
}

/**
 * Quote a path so the command lines this script prints survive a space in it.
 *
 * Single quotes, not JSON: inside double quotes a shell still expands `$(...)`,
 * a backtick, and a `\`, so a suggestion printed for a path containing them
 * would run something when it was pasted back.
 */
function shellQuote(value) {
    if (/^[A-Za-z0-9._/@:-]+$/.test(value)) return value;
    return "'" + value.split("'").join("'\\''") + "'";
}

const HELP = [
    'Usage: node scan.mjs [--root <dir>] [--json] [--no-git]',
    '',
    '  --root <dir>  Blueprints store to scan. Default: .blueprints',
    '  --json        Emit findings as JSON instead of a human summary.',
    '  --no-git      Skip git lookups; anchor revisions are reported, never expanded.',
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
    process.stdout.write('No Blueprints store found at ' + store.root + '\n');
    process.exit(0);
}

const { findings, repoRoot, gitAvailable } = analyze(store, { git: options.git });

const mechanical = findings.filter((f) => f.class === 'mechanical');
const needsAgent = findings.filter((f) => f.class === 'needs-agent');

const versions = new Map();
for (const item of store.items) {
    const value = item.blueprints || '(none)';
    versions.set(value, (versions.get(value) || 0) + 1);
}

if (options.json) {
    process.stdout.write(
        JSON.stringify(
            {
                root: store.root,
                repoRoot,
                gitAvailable,
                targetVersion: TARGET_VERSION,
                itemCount: store.items.length,
                versions: Object.fromEntries(versions),
                truncated: store.truncated,
                counts: { mechanical: mechanical.length, needsAgent: needsAgent.length },
                findings,
            },
            null,
            2,
        ) + '\n',
    );
    process.exit(0);
}

const groupByCode = (list) => {
    const groups = new Map();
    for (const finding of list) {
        const group = groups.get(finding.code) || [];
        group.push(finding);
        groups.set(finding.code, group);
    }
    return [...groups.entries()].sort((a, b) => b[1].length - a[1].length);
};

const out = [];
out.push('Blueprints upgrade scan -> ' + TARGET_VERSION);
out.push('Store: ' + store.root);
out.push('Items: ' + store.items.length);
out.push('Authored under: ' + [...versions.entries()].map(([v, n]) => v + ' (' + n + ')').join(', '));
if (!gitAvailable) {
    out.push('Git: ' + (options.git ? 'no repository found' : 'skipped') + ', anchor revisions will not be expanded');
}
if (store.truncated.files) out.push('WARNING: file limit reached; scan is incomplete');
if (store.truncated.depth) out.push('WARNING: depth limit reached; scan is incomplete');
out.push('');

const section = (title, list) => {
    out.push(title + ': ' + list.length);
    if (list.length === 0) {
        out.push('  none');
        out.push('');
        return;
    }
    for (const [code, group] of groupByCode(list)) {
        out.push('  [' + code + '] x' + group.length + '  (catalog change ' + group[0].change + ')');
        for (const finding of group) {
            const where = finding.line === null ? finding.item : finding.item + ':' + (finding.line + 1);
            out.push('    ' + where);
            out.push('      ' + finding.message);
        }
    }
    out.push('');
};

section('Mechanical (run fix.mjs)', mechanical);
section('Needs agent judgment', needsAgent);

if (mechanical.length > 0) {
    // Name fix.mjs the way the caller would have to type it, from wherever
    // scan.mjs was run, so the suggestion can be pasted back as printed.
    const fix = shellQuote(toPosix(relative(process.cwd(), fileURLToPath(new URL('fix.mjs', import.meta.url)))));
    const root = shellQuote(options.root);
    // Carry --no-git through: without it the suggested command would resolve
    // and rewrite the anchor revisions this scan deliberately left alone.
    const args = ' --root ' + root + (options.git ? '' : ' --no-git');
    out.push('Next: node ' + fix + args + '            # dry run');
    out.push('      node ' + fix + args + ' --write    # apply');
} else if (needsAgent.length === 0) {
    out.push('Nothing to upgrade.');
}

process.stdout.write(out.join('\n') + '\n');
