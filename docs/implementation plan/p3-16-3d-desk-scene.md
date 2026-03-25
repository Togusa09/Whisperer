# P3-16 Implementation Plan - 3D Desk Scene Integration

## Objective
Introduce an immersive 3D desk mode while preserving existing 2D flow.

## Scope
- Loadable 3D scene/panel for read/compose/send interactions.
- State-safe mode switching between 2D and 3D.

## Work Breakdown
1. Define 3D desk interaction model and required assets.
2. Build scene and interactive hotspots for read/compose/send.
3. Map current UI state into desk mode (letter content + draft state).
4. Implement safe toggle between modes without data loss.
5. Add play mode validation pass for input, camera, and state sync.

## Validation
- 3D desk loads and supports read/compose/send operations.
- Switching modes preserves current draft and timeline state.

## Risks
- Higher implementation cost from scene/UI state synchronization.
- Input mode conflicts and camera setup complexity.
