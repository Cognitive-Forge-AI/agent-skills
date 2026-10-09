---
name: write-content
description: Coordinate drafting, substantial revision, or adaptation when a writing task needs multiple capabilities. Use for complete content requests; route a standalone humanization, outline, or formatting task directly to its capability.
license: Apache-2.0
---

# Write content

Use only the capabilities needed to deliver the requested artifact. This skill coordinates work; sibling skills own their procedures. Mentioning a skill does not load it: read its SKILL.md before applying it. Resolve links relative to this file.

## Choose the work

- Paragraph humanization: read [humanizer](../humanizer/SKILL.md) only.
- Formatting only: read [format-writing](../format-writing/SKILL.md) only.
- Outline only: read [structure-writing](../structure-writing/SKILL.md) only.
- New or substantial writing: select the steps below according to gaps in the supplied material.

## Procedure

1. Reuse the supplied sources, audience, purpose, constraints, and selections. If facts need collecting, read [build-writing-context](../build-writing-context/SKILL.md). If requirements need establishing or profiles need resolving, read [define-writing-brief](../define-writing-brief/SKILL.md). Keep brief and context in conversation unless persistence helps.
2. For a multi-section artifact or structural revision, read structure-writing and obtain a useful outline. For drafting or stylistic revision, read humanizer. Load only chosen profile files through their owners, and pass their applicable rules and locked content to each capability.
3. Draft from supported material. Preserve exact examples, technical meaning, uncertainty, user voice, regional spelling, and required structure. Adaptation changes selection and expression for the new medium without inventing claims. Explicit requirements outrank profile defaults; surface incompatible mandatory requirements and ask for resolution before drafting affected content.
4. Apply format-writing when presentation needs work. Review the concerns that materially affect the task with [review-writing](../review-writing/SKILL.md). For complex or consequential artifacts, select independent reviewers when available and authorized. Give them the artifact, requirements, and relevant sources without expected findings or the writer's assessment.
5. Repair findings, compare changed claims and examples to the sources, and rerun affected review. Return the requested artifact with only material caveats or unresolved blockers.

## Roles and availability

Read [role dispatch](references/role-dispatch.md) only when delegating or performing a role-based review. It uses the four registered role files and provides the same sequential fallback when native subagents are unavailable.

A standalone install may lack siblings or selected profiles. Report a missing requested dependency; use supplied instructions and reasonable defaults for work that remains possible. Do not pretend an absent skill was applied or install personal skills automatically.
