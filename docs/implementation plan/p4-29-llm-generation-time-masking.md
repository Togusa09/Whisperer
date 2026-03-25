# P4-29 Implementation Plan - LLM Generation-Time Masking

## Objective
Hide raw model generation latency behind in-world delivery and async presentation.

## Scope
- Background generation with delivery scheduling.
- UI that presents transit outcomes, not model computation states.

## Work Breakdown
1. Separate generation completion from player-visible delivery events.
2. Add scheduler to release completed messages at in-world arrival times.
3. Replace blocking loader states with diegetic mail/telegraph cues.
4. Handle fallback paths for slow generation (reschedule, notify, retry policy).
5. Verify pause/resume behavior and timeline coherence.

## Validation
- Player-facing UI no longer exposes obvious generation waits for async flows.
- Delivery notifications align with game-time arrival windows.

## Risks
- Race conditions between generation completion and scheduler.
- Edge cases when player advances time faster than generation completes.
