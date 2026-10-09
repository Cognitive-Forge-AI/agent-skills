---
name: format-writing
description: Apply or audit headings, emphasis, lists, links, tables, code, and callouts when a writing task asks for presentation or markup changes. Use for formatting-only requests without starting a drafting workflow.
license: Apache-2.0
---

# Format writing

Make the supplied writing readable in its requested output format. Preserve meaning, wording, information order, technical identifiers, normative keywords, and exact examples unless the user also authorizes editing.

## Procedure

1. Identify the target syntax or renderer and locked formatting. A medium is not a syntax: a blog can use Markdown and documentation can use HTML. Reuse the supplied brief rather than creating one for a formatting-only task.
2. Read [Markdown conventions](references/profiles/markdown-conventions.md) only for selected Markdown presentation rules, and [technical text conventions](references/profiles/technical-text-conventions.md) only for selected technical presentation rules. Read other selected presentation files explicitly. Without a profile, follow the requested renderer's ordinary syntax and the artifact's established conventions.
3. Apply headings to existing information hierarchy, maintaining logical levels and required capitalization. Choose ordered lists for sequences and unordered lists for parallel items. Use a table when comparison benefits from columns. Do not create decorative headings or lists merely to make the artifact look formatted.
4. Distinguish emphasis, UI labels, identifiers, and executable samples within the selected conventions. Keep exact code unchanged inside inline code or a suitable block. Add descriptive link labels using provided destinations; do not invent URLs. Supply purposeful image alternatives from available information and flag missing descriptions. Use renderer-supported callouts for consequences or prerequisites, without hiding essential steps inside them.
5. Check escaping, fence boundaries, link syntax, hierarchy, and whether formatting changed meaning. Explicit requirements override defaults within higher-priority instructions. When mandatory rules are incompatible, state the conflict and request a choice before changing the affected text; continue independent formatting only if useful.

## Output and boundaries

Return the formatted artifact for an apply request, or location, rule, and concrete repair for an audit. Explain a renderer limitation only when it affects use. Do not load context collection, structure, or prose skills for formatting alone. If additional prose editing is requested, read [humanizer](../humanizer/SKILL.md) first and preserve the locked presentation constraints.
