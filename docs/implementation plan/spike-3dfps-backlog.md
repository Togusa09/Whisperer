# 3D/FPS Spike Backlog (Secondary)

## Purpose
This backlog tracks the 3D first-person investigation work on the spike branch only.
It does not replace or re-prioritize the main implementation backlog.

## Scope
- Validate a one-scene first-person exploration loop in Wilmarth's study.
- Validate a same-scene control-mode transition from FPS exploration to constrained desk interaction.
- Validate a physical letter flow: mailslot drop -> floor pickup -> carry to desk -> open/read at desk.

## Non-Goals
- Production-ready narrative branching and async orchestration.
- Replacing the existing 2D correspondence loop.
- Modifying vendor/reference-only code paths.

## Status Legend
- Planned: not started.
- In Progress: active implementation.
- Done: validated against acceptance criteria.
- Decision Needed: blocked pending spike decision.

## Linked Main-Backlog Items (Copied for Spike Tracking)

### L1) Main #16 - 3D desk scene integration (Spike Adaptation)
Priority: P3
Effort: L
Source: [docs/implementation-backlog.md](../implementation-backlog.md)
Source Plan: [p3-16-3d-desk-scene.md](p3-16-3d-desk-scene.md)
Dependencies: 2
Status: In Progress
Spike Goal:
- Deliver an investigation-quality architecture and vertical slice for same-scene FPS/Desk mode switching.
Acceptance Criteria:
- Wilmarth study scene supports FPS exploration and desk-mode entry in one scene.
- Switching modes does not lose active draft or turn context.

### L2) Main #17 - Send/receive animations (Spike Adaptation)
Priority: P3
Effort: M
Source: [docs/implementation-backlog.md](../implementation-backlog.md)
Source Plan: [p3-17-send-receive-animations.md](p3-17-send-receive-animations.md)
Dependencies: 2, 15
Status: Planned
Spike Goal:
- Define and prototype letter-arrival staging in 3D space (mailslot to floor), with clear state completion points.
Acceptance Criteria:
- Arrival event can be represented physically and reaches a stable interactable end state.
- Skip/fallback behavior is defined for future animation implementation.

### L3) Main #27 - Message and mail service types (Spike Adaptation)
Priority: P4
Effort: L
Source: [docs/implementation-backlog.md](../implementation-backlog.md)
Source Plan: [p4-27-message-service-types.md](p4-27-message-service-types.md)
Dependencies: 1, 2
Status: Planned
Spike Goal:
- Keep service-type compatibility in the design so the 3D delivery points can support multiple mail channels later.
Acceptance Criteria:
- Spike architecture identifies extension points for service-specific arrival rules.

### L4) Main #28 - Fake/missing correspondence (Spike Adaptation)
Priority: P4
Effort: M
Source: [docs/implementation-backlog.md](../implementation-backlog.md)
Source Plan: [p4-28-fake-missing-correspondence.md](p4-28-fake-missing-correspondence.md)
Dependencies: 1, 27, 4
Status: Planned
Spike Goal:
- Preserve data/state boundaries so in-world letter objects can diverge from authoritative ledger state later.
Acceptance Criteria:
- Spike state model identifies where player-visible and authoritative correspondence states can fork safely.

### L5) Main #29 - Generation-time masking (Spike Adaptation)
Priority: P4
Effort: M
Source: [docs/implementation-backlog.md](../implementation-backlog.md)
Source Plan: [p4-29-llm-generation-time-masking.md](p4-29-llm-generation-time-masking.md)
Dependencies: 27, 28, 2
Status: Planned
Spike Goal:
- Ensure the 3D flow does not expose raw model latency in future async delivery mode.
Acceptance Criteria:
- Player-facing delivery stages can be decoupled from raw generation timing.

## Spike-Only Tasks

### S1) Scene Readiness and Spawn Marker Wiring
Priority: P3
Effort: S
Dependencies: none
Status: In Progress
Description:
- Validate the study scene has a reliable player spawn marker and scene references for desk and mailslot interaction zones.
Acceptance Criteria:
- Scene has a clearly identified spawn marker consumed by runtime bootstrap.
- Missing required references report explicit warnings.

### S2) FPS Baseline Controller Integration
Priority: P3
Effort: M
Dependencies: S1
Status: In Progress
Description:
- Integrate movement/look/interaction input actions for exploration mode.
Acceptance Criteria:
- Player can move and look around study in FPS mode.
- Exploration mode can be enabled/disabled by central mode switcher.

### S3) Same-Scene Desk Mode Transition
Priority: P3
Effort: M
Dependencies: S2
Status: In Progress
Description:
- Add central state manager that switches between Explore and Desk modes in one scene.
Acceptance Criteria:
- Entering desk mode constrains controls and camera behavior.
- Exiting desk mode restores exploration controls predictably.

### S4) Physical Letter Arrival Slice (Mailslot to Floor)
Priority: P3
Effort: M
Dependencies: S1, S3
Status: In Progress
Description:
- Stage physical arrival from door mailslot and settle letter on floor as interactable object.
Acceptance Criteria:
- Letter can be spawned at mailslot anchor and becomes pickup-eligible after settling.

### S5) Pickup, Carry, and Desk-Only Open Rule
Priority: P3
Effort: M
Dependencies: S3, S4
Status: In Progress
Description:
- Player picks up letter from floor, carries it, and can open/read only while in desk context.
Acceptance Criteria:
- Open/read action is rejected outside desk context.
- Opened letter remains available at desk only for this phase.

### S6) State Bridge Between 3D Interaction and Existing UI Loop
Priority: P3
Effort: M
Dependencies: S3, S5
Status: Planned
Description:
- Define and prototype adapter boundaries between world interaction state and existing correspondence controller state.
Acceptance Criteria:
- Integration points are explicit and testable without rewriting current correspondence controller.

### S7) Validation and Decision Review
Priority: P3
Effort: S
Dependencies: S1, S2, S3, S4, S5, S6
Status: Planned
Description:
- Run focused validation for the vertical slice and capture go/no-go decision.
Acceptance Criteria:
- Manual runbook passes critical flow.
- Decision log captures outcomes, risks, and next implementation recommendation.

## Milestones

### M1 - Foundations
Tasks: S1, S3
Exit Criteria:
- Scene references are wired.
- Central mode switching exists and can be toggled.

### M2 - Exploration and Transition
Tasks: S2, S3
Exit Criteria:
- FPS exploration works.
- Desk mode entry/exit is stable.

### M3 - Letter Vertical Slice
Tasks: S4, S5, S6
Exit Criteria:
- End-to-end flow works: mailslot arrival -> pickup -> desk open/read.

### M4 - Validation and Decision
Tasks: S7
Exit Criteria:
- Results documented.
- Production recommendation recorded.

## Implementation Notes (2026-03-26)

Current branch implementation added first-party runtime scaffolding for M1/M2 and part of M3:
- `Assets/_Game/Scripts/Core/PlayerMode.cs`
- `Assets/_Game/Scripts/Core/PlayerSpawnMarker.cs`
- `Assets/_Game/Scripts/Core/PlayerModeSwitcher.cs`
- `Assets/_Game/Scripts/Core/FirstPersonMover.cs`
- `Assets/_Game/Scripts/Core/PlayerInteractionController.cs`
- `Assets/_Game/Scripts/Core/StudyInteractable.cs`
- `Assets/_Game/Scripts/Core/DeskModeInteractable.cs`
- `Assets/_Game/Scripts/Core/LetterItem.cs`
- `Assets/_Game/Scripts/Core/LetterArrivalController.cs`

Implemented behavior in this slice:
- Explore vs Desk mode state and camera/behaviour toggling.
- Spawn marker-based player placement at startup.
- FPS movement/look/jump/sprint baseline (keyboard+mouse controls).
- Interaction raycast (`E`) with desk-mode entry and desk-mode exit (`Esc`).
- Mail pickup/drop and desk-only open placement constraint.
- Envelope spawn at the door, with desk opening that reveals a letter on the desk anchor.
- Door spawn controller with physical impulse for floor-drop testing.

Unity scene integration completed in `WilmarthStudy.unity`:
- Created `PlayerRig` with `CharacterController`, `FirstPersonMover`, `PlayerModeSwitcher`, and `PlayerInteractionController`.
- Re-parented `Main Camera` under `PlayerRig` and created `CarryAnchor`.
- Added `DeskCamera` for desk-mode viewing.
- Added `PlayerSpawnMarker` to `PlayerSpawn`.
- Added `DeskModeInteractable` to `Desk/Work Area`.
- Added `LetterArrivalController` to `Door` with `EnvelopeSpawn` as spawn point.
- Restored `PlayerRig` scene wiring after it was lost from the saved scene during spike iteration.
- Added `LetterItem` to the physical child object inside `Assets/_Game/Prefabs/Letter.prefab`.
- Added `EnvelopeItem` to the physical child object inside `Assets/_Game/Prefabs/Envelope.prefab`, configured to reveal `Letter.prefab` at the desk.

Current debug controls:
- `WASD`: move
- Mouse: look
- `Left Shift`: sprint
- `Space`: jump
- `E`: interact / pick up / drop / open at desk
- `Esc`: exit desk mode
- `R`: spawn incoming envelope at door spawn marker

Scene wiring checklist for current scripts:
1. Add `PlayerModeSwitcher` to a scene coordinator object and assign:
	- `playerRoot`
	- `explorationCamera`
	- `deskCamera`
	- `spawnMarker` (optional if unique `PlayerSpawnMarker` exists)
2. Add `FirstPersonMover` to the player object with a `CharacterController`.
3. Add `PlayerInteractionController` to the player object and assign:
	- `interactionCamera`
	- `carryAnchor` (usually a child transform near camera)
	- `deskLetterAnchor` (where opened letters rest at desk)
	- `modeSwitcher`
4. Add `DeskModeInteractable` to desk interaction collider(s).
5. Add `LetterItem` to letter prefab root so pickup/open behavior is enabled.

Known gap before S4 completion:
- Delivery timing/state is still debug-driven; it is not yet connected to correspondence/UI state or authored arrival sequencing.

## Decision Log

### D1) Desk mode in same scene
Decision: Approved
Rationale:
- Avoids scene state transfer complexity during core interaction loop.

### D2) Spike backlog separated from main backlog
Decision: Approved
Rationale:
- Prevents exploratory volatility from disrupting primary delivery planning.
