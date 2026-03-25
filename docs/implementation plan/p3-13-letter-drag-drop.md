# P3-13 Implementation Plan - Letter Drag and Drop

## Objective
Let the player take selected letters out onto the desk and rearrange them freely during play, without breaking reading or writing interactions.

## Design Framing
This feature should be presented in-world as handling correspondence on a desk rather than managing modern desktop windows.

Preferred player-facing metaphors:
- Taking a letter out from the file drawer or archive.
- Laying letters on the desk for reference.
- Returning a letter to the file drawer.
- Keeping active correspondence spread out while drafting.

Avoid user-facing terminology such as:
- window
- tab
- panel manager
- desktop app

Internally, the implementation can still use floating UI elements and drag behavior.

## Scope For This Item
This ticket should deliver the smallest useful slice of the future desk workflow.

Included in #13:
- A draggable received-letter surface that can be repositioned on-screen during the current play session.
- A clear "take out / put away" interaction for that letter surface.
- Session-only persistence of current desk position while the scene remains loaded.
- A controller-side foundation that can later support more than one open letter at once.

Explicitly not included in #13:
- Multiple simultaneously open received letters.
- A full filing-drawer browser with stacks, categories, or search.
- Duplication of the composer and archive into separate floating correspondence workspaces.
- Timeline-driven delivery shelves, message service handling, or fake/missing mail reveals.
- 3D desk presentation.

## Product Intent
The long-term goal is not merely draggable UI. The goal is a correspondence workflow where the player can:
- review past letters,
- pull selected letters out for reference,
- arrange them on the desk while composing,
- and later manage several open pieces of correspondence at once.

This ticket should therefore build a reusable floating-letter pattern, but stop short of the full desk management system.

## Recommended Implementation Slice
For #13, implement a single floating received-letter card/dialog with diegetic open/close behavior.

Recommended MVP behavior:
1. The latest received letter can be opened from the existing archive/notification affordance.
2. Once opened, the letter appears as a movable desk item.
3. The player drags it by a non-text handle area so reading and text selection still work.
4. The player can close/put away the letter, removing it from the desk.
5. Reopening the same letter during the same session restores its last desk position.

This gives the project a real desk-item interaction model without forcing the full multi-letter system into the first slice.

## Why This Is The Right Cut
Trying to make every major UI region draggable in this ticket would conflate two separate problems:
- introducing draggable correspondence objects, and
- redesigning the entire layout into a freeform desk workspace.

The first problem belongs here. The second should be deferred until the desk interaction model is broader and the diegetic archive flow is better defined.

## Proposed UX Language
Use era-grounded labels and prompts such as:
- "Take Letter Out"
- "Lay on Desk"
- "Return to File"
- "Filed"
- "On Desk"

Avoid explicit labels like:
- "Open Window"
- "Close Window"
- "Dock"
- "Undock"

## Work Breakdown
1. Define a small "desk letter" interaction model.
	- Treat an opened received letter as a desk item with state such as `Filed` or `OnDesk`.
	- Keep this model local and lightweight for now; do not introduce a full correspondence workspace system yet.
2. Add drag-safe interaction zones.
	- Use a header, title bar, or paper-edge region as the drag handle.
	- Ensure body text, selection, scrolling, and buttons remain non-drag zones.
3. Implement pointer drag behavior.
	- Track pointer down/move/up.
	- Translate the floating letter surface within the visible play area.
	- Clamp movement to bounds so the item cannot be lost off-screen.
4. Support minimal open/close semantics.
	- "Take out" creates or shows the floating letter on the desk.
	- "Return to file" hides it and preserves enough local state to reopen it in the same session.
5. Add session-only state tracking.
	- Track which correspondence item is currently on the desk.
	- Track its last known position.
	- Reset on scene reload or new play session.
6. Prepare for future multi-letter expansion.
	- Encapsulate drag behavior and desk-position state so the system can later support a collection of open letters instead of just one.
	- Prefer reusable controller methods or a small helper/manipulator over one-off popup logic.
7. Validate archive-to-desk flow.
	- The player should be able to select a historical letter, take it out, read it, return it, and continue drafting without losing context.

## Minimal Technical Shape
The current popup-based received-letter view is a good starting point.

Recommended implementation direction:
- Keep the current two-column composer/archive layout intact.
- Reuse the received-letter popup as the first floating desk letter.
- Add drag behavior to that floating letter only.
- Add lightweight state for whether the current letter is filed or on the desk.
- Structure code so the popup controller can later be generalized into spawned desk-letter instances.

This avoids destabilizing the main layout while still moving toward the intended multi-letter future.

## Future Expansion Split
The broader correspondence-desk vision should be divided across later tasks.

### What stays in #13
- First draggable floating letter surface.
- Diegetic "take out / return to file" interaction.
- Session-only desk position persistence.
- Reusable drag/floating-letter foundation.

### What overlaps with #14 Multi-Draft Compose Workflow
- Distinguishing unsent drafts from archived received letters.
- Surfacing multiple player-authored draft documents for reference.
- Preventing confusion between active desk items and draft versions.

Keep #14 focused on authored draft management, but design #13 so desk items and draft items can later coexist.

### What overlaps with #16 3D Desk Scene Integration
- Physical desk presentation.
- Spatial props such as drawers, stacks, blotters, envelopes, and writing tools.
- More tactile "take from drawer / place on desk" interactions.

In #13, emulate this behavior in 2D UI only. Do not build 3D affordances yet.

### What overlaps with #27 Message and Mail Service Types
- Different correspondence types entering the archive/drawer.
- Service-specific labels, postmarks, transit handling, and arrival semantics.

In #13, assume ordinary letters only and avoid building service-specific desk logic.

### What overlaps with #28 Fake and Missing Correspondence
- Archive truth versus player-visible filing state.
- Hidden, delayed, or suspicious items appearing in the drawer.

In #13, keep filing state simple and trustworthy. Do not mix diegetic uncertainty into the first draggable-letter implementation.

### What overlaps with #29 LLM Generation-Time Masking
- Releasing correspondence into the player's filing flow only when it arrives in-world.
- Presenting delivery as mail handling rather than raw generation completion.

In #13, allow the existing reply-ready flow to place a letter onto the desk model without solving full async scheduling.

## Data / State Guidance
Keep the first implementation intentionally small.

Suggested transient state:
- correspondence entry id or archive index for the currently opened letter
- desk state: filed / on-desk
- last desk position for opened letter
- optional z-order token if future stacking is desired

Design this state so it can later become:
- a map of correspondence id to desk position/state
- a list of currently open letters on the desk

without rewriting the drag behavior from scratch.

## Validation
- The player can take the latest received letter out onto the desk.
- The letter drags smoothly and remains within visible bounds.
- Text selection, scrolling, and reading still work in non-drag zones.
- Returning the letter to the file removes it from the desk cleanly.
- Reopening the same letter in the same session restores its last desk position.
- Draft composition and archive browsing continue to work without interruption.

## Risks
- Input conflicts between dragging, scrolling, and text selection.
- The implementation may become too popup-specific and be hard to generalize later.
- Naming or labels may accidentally slip into modern desktop terminology.
- If multi-letter support is half-started here, the ticket may expand beyond its intended scope.

## Success Criteria For This Ticket
This ticket is successful if it delivers a convincing first "desk letter" interaction, not a full correspondence workspace.

By the end of #13, the player should feel like they can pull a letter from the file, lay it on the desk, move it around, and put it away again. The rest of the multi-letter filing system should remain clearly staged for later tasks.
