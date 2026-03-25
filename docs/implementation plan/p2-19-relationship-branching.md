# P2-19 Implementation Plan - Relationship-Driven Branching

## Objective
Drive disclosures, tone, and outcomes from Stability/Trust state bands.

## Scope
- Deterministic branch selection by meter ranges and timeline.
- In-world feedback signals for player inference.

## Work Breakdown
1. Define narrative state bands and transition rules.
2. Tag content/retrieval events by required meter thresholds.
3. Implement deterministic branch resolver.
4. Surface subtle cues in letters/archive for state inference.
5. Add deterministic replay tests across fixed seeds/timelines.

## Validation
- At least three distinct narrative states are reachable.
- Same inputs yield same branch outcomes.

## Risks
- Branch explosion and content maintenance burden.
- Ambiguous feedback that players cannot read.
