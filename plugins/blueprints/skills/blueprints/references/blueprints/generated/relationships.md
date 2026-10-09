# Generated Blueprints relationships

Suite version: 0.2.0-preview.1

Generated from the Core, Project Memory, and Working Memory definition modules. This is the available authoring vocabulary; project selections determine which labels apply. Edit the source specifications, then run `node scripts/build-references.mjs`.

| From | To | Label | Reciprocal | Level | Reason | Reciprocal rule | Module |
| --- | --- | --- | --- | --- | --- | --- | --- |
| item | item | `decomposes-into` | `part-of` | SHOULD | OPTIONAL | SHOULD | [core](../core.md) |
| item | item | `part-of` | `decomposes-into` | SHOULD | OPTIONAL | SHOULD | [core](../core.md) |
| item | artifact | `anchored-to` | none | SHOULD | OPTIONAL | none | [core](../core.md) |
| spec | spec | `related-to` | `related-to` | SHOULD | REQUIRED | SHOULD | [project-memory](../extensions/project-memory.md) |
| spec | spec | `related-to` | `related-to` | SHOULD | REQUIRED | SHOULD | [project-memory](../extensions/project-memory.md) |
| spec | spec | `depends-on` | `required-by` | SHOULD | REQUIRED | SHOULD | [project-memory](../extensions/project-memory.md) |
| spec | spec | `required-by` | `depends-on` | SHOULD | REQUIRED | SHOULD | [project-memory](../extensions/project-memory.md) |
| decision | spec | `affects` | `affected-by` | SHOULD | OPTIONAL | SHOULD | [project-memory](../extensions/project-memory.md) |
| spec | decision | `affected-by` | `affects` | SHOULD | OPTIONAL | SHOULD | [project-memory](../extensions/project-memory.md) |
| decision | decision | `supersedes` | `superseded-by` | MUST | OPTIONAL | MUST | [project-memory](../extensions/project-memory.md) |
| decision | decision | `superseded-by` | `supersedes` | MUST | OPTIONAL | MUST | [project-memory](../extensions/project-memory.md) |
| exploration | spec | `explores` | none | SHOULD | OPTIONAL | none | [working-memory](../extensions/working-memory.md) |
| exploration | decision | `informs` | none | SHOULD | OPTIONAL | none | [working-memory](../extensions/working-memory.md) |
