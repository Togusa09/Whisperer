# Spike - Configurable Correspondence Profiles

## Status: Proposed

## Objective
Investigate making character portrayal configurable so the same correspondence mechanics can support other Lovecraft-era character pairs, while preserving the existing monthly letter loop and chronology rules.

## Why This Spike Exists
Current implementation is strongly tuned to Albert N. Wilmarth <-> Henry W. Akeley. Names, addresses, role framing, and portions of writing guidance are hardcoded in runtime code.

A profile-driven approach would allow:
- Different character pairs.
- Different locations and local context.
- Different writing voice constraints.
- Different horror framing/escalation style.

Without changing:
- Turn cadence (one turn = one month).
- Send-date vs reply-date logic.
- Chronology consistency checks.
- Historical period guardrails and real-world cutoff logic.

## Included In This Spike (Investigation Scope)
- Define an editor-time configurable profile model for correspondence portrayal.
- Identify refactors needed to replace hardcoded names/addresses/style text with profile fields.
- Identify how timeline defaults and seed content should be sourced per profile.
- Define validation and acceptance tests for profile-driven behavior.
- Estimate implementation effort, risks, and ordering.

## Not Included At This Time
- Runtime in-session profile switching.
- Save-system redesign or migration across profile changes.
- Full freeform prompt-template editor UI.
- Broad non-Lovecraft genre support.
- Changes to vendor/reference-only content.

## Reasoning For Scope Boundaries
- Editor-time profile selection delivers most value with lower risk and less UI complexity.
- Keeping mechanics fixed protects existing gameplay loop and avoids chronology regressions.
- Avoiding runtime switching defers state-persistence and migration concerns.
- Preset style fields provide flexibility while maintaining controllable prompt quality.
- Remaining Lovecraftian keeps content constraints coherent with current design goals.

## Constraints To Preserve
- Valid time periods should remain within eras used by Lovecraft's fiction and human historical presence.
- No modern knowledge beyond configured cutoff year.
- Responses must remain period-appropriate letters, not chat-style dialogue.
- Existing chronology retrieval and consistency validation must remain authoritative.

## Implementation Plan
1. Create a profile contract (ScriptableObject):
- Character names and addresses.
- Initial relationship state values and display labels.
- Style preset fields (tone, diction, pacing guidance).
- Horror preset fields (threat framing, escalation cues, taboo details policy).
- Timeline defaults (start date, reply day, knowledge cutoff year).
- Seed content references (starting correspondence/story events).

2. Wire profile into runtime systems:
- Update LetterUiController to read portrayal text/labels/placeholders from profile.
- Update LetterPromptBuilder to inject profile-driven role and style framing.
- Apply timeline defaults from profile into GameTimeManager initialization.
- Route seed data loading through profile-selected resources.

3. Add validation:
- Required fields and date-range sanity checks.
- Cutoff-year and period constraints.
- Style/horror preset completeness checks.

4. Verify parity and variation:
- Regression pass with current Akeley/Wilmarth profile (behavior parity).
- Multi-turn pass with alternate profile (portrayal changes only).
- Confirm cadence, chronology checks, and retrieval behavior remain unchanged.

5. Document rollout path:
- Keep existing hardcoded defaults as fallback while migrating.
- Ship with at least one baseline profile plus one alternate profile for validation.

## Estimated Effort (If Approved)
- Profile contract + defaults: 0.5 to 1 day.
- Runtime refactor (controller + prompt wiring): 1.5 to 2.5 days.
- Validation + authoring polish: 0.5 to 1 day.
- QA/tuning across two profiles: 0.5 to 1 day.
- Total: approximately 3 to 5 working days.

## Candidate Touchpoints
- Assets/_Game/Scripts/UI/LetterUiController.cs
- Assets/_Game/Scripts/Prompting/LetterPromptBuilder.cs
- Assets/_Game/Scripts/Core/GameTimeManager.cs
- Assets/_Game/Scripts/Core/StoryEventLedger.cs
- Assets/_Game/Scripts/Editor/WhispererContentPackAuthoringWindow.cs
- Assets/_Game/README.md

## Risks
- Prompt quality drift when moving from bespoke to profile-driven wording.
- Hidden assumptions in relationship state labels and UI copy.
- Profile/content mismatches (names in events/tags not aligned with selected profile).

## Exit Criteria For Spike
- Clear approved profile schema.
- Clear approved migration plan.
- Confirmed effort estimate and sequencing.
- Decision recorded on whether to proceed to implementation ticket(s).
