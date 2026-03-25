# P4-28 Implementation Plan - Fake and Missing Correspondence

## Objective
Introduce controlled uncertainty via fake received mail and missing sent mail.

## Scope
- Fake received messages unknown to LLM sender state.
- Missing outbound messages not delivered to player.

## Work Breakdown
1. Extend message state model (received, in-transit, missing, fake).
2. Add author/debug injection tools for fake/missing events.
3. Implement delivery suppression and late-reveal pathways.
4. Ensure ledger chronology records authoritative truth.
5. Add safeguards and player-facing affordances to avoid confusion spikes.

## Validation
- Fake and missing events can be authored and replayed deterministically.
- Archive can reveal truth only when configured conditions are met.

## Risks
- Player trust loss if signals feel unfair.
- Continuity issues if hidden events leak too early.
