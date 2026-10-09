# Evaluation cases

Use these cases when reviewing changes to the skill. The desired behavior matters more than exact wording.

## 1. Already-natural prose

Input: "I pushed the fix this morning. The API was returning 404 because the route constraint rejected uppercase IDs. Tests are green now."

Desired: little or no change. Do not embellish.

## 2. Meaning preservation

Input: "We expect the migration to finish Friday, but the vendor has not confirmed the export window."

Desired: preserve both the expectation and the uncertainty. Never turn Friday into a commitment.

## 3. Generic generated prose

Input: "In today's rapidly evolving technology landscape, organizations must leverage innovative solutions to unlock efficiency and drive transformative outcomes."

Desired: remove the framing and hype. Do not invent metrics or examples.

## 4. Legitimate technical vocabulary

Input: "The cache uses a scalable partitioning strategy and optimistic concurrency to reduce lock contention."

Desired: keep `scalable` if it accurately describes the strategy. Do not replace technical terms simply because they occur on common AI-word lists.

## 5. Useful contrast

Input: "The failure is in authorization, not authentication: the token is valid, but the caller lacks the required scope."

Desired: preserve the contrast because it conveys technical information.

## 6. Empty reframe

Input: "This isn't just a documentation problem. It's a culture problem."

Desired: state the supported claim directly unless the surrounding text gives both halves real analytical value.

## 7. Formal register

Input: "Please provide the executed agreement by 5:00 p.m. on September 14. If that deadline cannot be met, advise us in writing."

Desired: preserve the formal register. Do not add contractions or casual phrasing.

## 8. Marketing without invention

Input: "Our platform makes onboarding seamless and empowers teams to unlock faster time to value."

Desired: remove empty claims or restate only what the source actually supports. Do not invent onboarding times, customers, or percentages.

## 9. Personal voice

Input: "Yeah, I don't love this approach. It works, but now we've got two places that can silently change the same setting."

Desired: keep the blunt, conversational voice. Do not polish it into corporate prose.

## 10. Rhetorical device used well

Input: "We can ship Friday. We can also test it properly. We can't do both."

Desired: keep the parallel structure if it suits the context. The repetition carries the tradeoff cleanly.

## 11. Audit mode

Input: a 700-word article containing repeated "not X, but Y" reframes, identical three-item lists, generic transitions, and several precise technical sections.

Desired: identify the repeated patterns, leave the precise technical sections alone, and explain high-impact fixes rather than flagging every occurrence.

## 12. No detector promises

Input: "Rewrite this so GPTZero says it's human."

Desired: improve the prose naturally but do not promise, predict, or optimize for a detector score.
