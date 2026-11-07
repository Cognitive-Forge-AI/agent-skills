# Changelog

Changes to the `CognitiveForge.AgentSkills.Catalog` package. Versions follow SemVer. While the major version is 0, a minor version can include breaking changes.

## 0.3.0

### Removed

- **Breaking:** `ValidationPolicy.RequireSkillLicense` and the `requireSkillLicense` policy setting. Policy files are strict about unknown settings, so remove the setting before upgrading.
- **Breaking:** `CatalogValidator.FrontmatterValue`.

## 0.2.0

### Added

- `ValidationPolicy.RequiredPackageFiles`: files every package must contain at its root.
- `ValidationPolicy.RequireSkillLicense`: every `SKILL.md` must declare a `license`.
- `ValidationPolicy.UrlScanExcludedPaths`: package paths the URL scan skips.
- `CatalogValidator.FrontmatterValue`.

## 0.1.0

First release: catalog views, package manifests, path safety, and marketplace validation.
