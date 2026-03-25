# P3-15 Implementation Plan - Letter Composition Animations

## Objective
Animate incoming letter reveal and prepare optional hooks for future voice/read-aloud.

## Scope
- Character-by-character reveal with speed controls.
- Optional typed style variant for later story phases.

## Work Breakdown
1. Add reusable text reveal component (start/skip/complete).
2. Bind component to incoming Akeley letter display.
3. Add user-facing speed setting and default profile.
4. Add optional typed-style preset switch by story state.
5. Expose event hooks for future audio narration integration.

## Validation
- Incoming text reveals smoothly and can be skipped.
- Speed setting changes visible behavior immediately.

## Risks
- Performance stutter on long letters.
- Timing conflicts with send/receive transition flows.
