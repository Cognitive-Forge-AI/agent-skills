# Maintaining project memory

Load for approved or historically resolved items. First distinguish clarification from a change in meaning.

- Clarifying wording: update the same item without manufacturing a new choice or history entry.
- Changed desired behavior: update the same Spec under the project's review authority. Keep the body about current desired state; review incoming and outgoing relationship reasons.
- Investigating another option: preserve the accepted Decision and active intent. A request to investigate is not approval to replace them.
- Substantive replacement choice: author a separate proposed Decision. Only when it is accepted, record both `supersedes` and `superseded-by`, change the old status to `superseded`, and review affected Specs.
- Abandoned decision problem or proposal: use `withdrawn` for a Decision still in `draft` or `proposed`, keeping its question or proposal and recording why work stopped. A draft with no useful context can be deleted after repairing incoming links. Never delete or withdraw an accepted, rejected, or superseded record.
- Withdrawn intent: preserve the Spec identity and use `removed` after the applicable project authorization.

Preserve historical reasoning instead of silently rewriting it to match the newest code. Code owns implementation, so a mismatch with desired intent is drift to report and resolve. A proposal is not current authority.

Before any write, check the owning extension's mode. Reference-only items remain unchanged during ordinary work, even when adding a recommended reciprocal would be convenient. A mandatory reciprocal is a real constraint and cannot be waived. Resolve the policy or reference choice explicitly.

Report actual validation results and limitations in the task or change review. Do not create an item merely to document that an edit or review occurred. Follow valid sources instead of copying their full content.
