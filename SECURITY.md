# Security policy

## Reporting a vulnerability

Email [opensource@cognitiveforge.ai](mailto:opensource@cognitiveforge.ai) with a private report. Include the affected plugin and version, or the commit, along with the impact and a minimal reproduction. Do not post secrets or exploit details in public issues or pull requests. If GitHub private vulnerability reporting is enabled, you may use the repository's **Security** tab instead.

Maintainers investigate reports as capacity permits and agree on disclosure timing privately. There is no fixed response deadline or bounty. If a credential was exposed, revoke or rotate it promptly. Deleting a file from the latest revision does not remove it from history or from other copies.

## Supported versions

Fixes target `main` and the latest published version of each plugin. A fix ships as a new plugin version; existing versions are never changed in place.

## Trust boundaries

Plugins supply agent instructions and can include executable scripts, hooks, commands, MCP servers, apps, and assets. Installation uses each harness's native commands. Review the entire package and any external service access before enabling it, and run agents with access appropriate for your task.

Validation checks paths, links, manifests, licenses, catalog consistency, and policy URLs. It does not sandbox plugin execution or audit the meaning of every instruction. Releases are opened as pull requests, scanned for secrets, validated, and reviewed by a maintainer before merging. Pull request checks run with read-only repository permission and no publishing credentials.
