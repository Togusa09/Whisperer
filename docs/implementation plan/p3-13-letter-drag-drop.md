# P3-13 Implementation Plan - Letter Drag and Drop

## Objective
Allow letter cards/panels to be repositioned during a session without breaking text interactions.

## Scope
- Pointer drag behavior for selected letter surfaces.
- Session-only persistence for positions.

## Work Breakdown
1. Add drag handles or drag zones to avoid text-selection conflict.
2. Implement pointer down/move/up tracking and panel translation.
3. Clamp movement to viewport bounds.
4. Store positions in session memory and reset on scene reload.
5. Add interaction tests for drag vs. text selection.

## Validation
- Panels drag smoothly and stop at window bounds.
- Text selection still works in non-drag zones.

## Risks
- Input conflicts with existing click handlers.
- Z-order and hit testing issues.
