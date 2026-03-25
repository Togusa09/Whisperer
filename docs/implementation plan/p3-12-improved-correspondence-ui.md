# P3-12 Implementation Plan - Improved Correspondence UI

## Objective
Deliver a stable three-column correspondence layout: Received | Composer | Archive.

## Scope
- Responsive 3-panel layout with clear reading hierarchy.
- Keep draft editing uninterrupted while archive browsing.

## Work Breakdown
1. Refactor UXML hierarchy into three major containers.
2. Update USS for balanced column widths and responsive collapse behavior.
3. Ensure received-letter pane prioritizes readability (line length, scroll behavior).
4. Keep archive interactions non-modal and draft-safe.
5. Add play mode UI checks for common resolutions.

## Validation
- Latest received letter remains visible while composing.
- Archive navigation does not clear or block current draft.
- Layout remains usable on 16:9 and narrower window sizes.

## Risks
- Layout regressions in UI Toolkit style overrides.
- Scroll conflicts between nested views.
