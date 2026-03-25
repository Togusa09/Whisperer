# P3-17 Implementation Plan - Send/Receive Animations

## Objective
Animate outgoing and incoming correspondence transitions to strengthen feedback.

## Scope
- Outgoing sequence: seal/address/send.
- Incoming sequence: delivery/arrival/opening.
- Skip and timeout-complete support.

## Work Breakdown
1. Design animation timelines and transition states.
2. Implement outgoing sequence with completion callbacks.
3. Implement incoming sequence with notification trigger.
4. Add skip interaction and fallback auto-complete timeout.
5. Verify behavior with async messaging states.

## Validation
- Outgoing and incoming sequences run end-to-end.
- Skip always reaches a correct final UI state.

## Risks
- Animation state machine dead-ends.
- Mismatch between visual completion and data/state completion.
