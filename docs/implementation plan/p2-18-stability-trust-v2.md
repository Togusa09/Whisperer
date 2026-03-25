# P2-18 Implementation Plan - Stability and Trust Model v2

## Objective
Replace simple keyword heuristics with tunable scoring based on intent and tone.

## Scope
- Hidden Stability and Trust values updated each turn.
- Explainable per-turn delta trace for tuning.

## Work Breakdown
1. Define scoring factors (intent class, sentiment/tone, trigger phrases, event context).
2. Externalize weights/thresholds to data config.
3. Implement turn-scoring pipeline and meter updates.
4. Emit structured debug trace for each score contribution.
5. Add balancing scenarios and regression tests.

## Validation
- Meter changes respond to intent/tone beyond keyword overlap.
- Designers can tune without code changes.

## Risks
- Overfitting to test prompts.
- Non-obvious interactions among weighted factors.
