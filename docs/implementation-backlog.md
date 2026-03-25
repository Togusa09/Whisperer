# Whisperer Implementation Backlog

## Scope

This backlog is for the first playable narrative loop:
- Player writes as Albert N. Wilmarth.
- LLM replies as Henry W. Akeley.
- One turn advances one month.
- Reply context is mid-month.
- Chronology and 1930 knowledge constraints are enforced.

## Delivery Plan

### Milestone A - Core Loop
Goal: Replace placeholder chat flow with a letter-based loop and deterministic time state.

### Milestone B - Chronology RAG
Goal: Retrieve only time-valid narrative context.

### Milestone C - Expanded Knowledge
Goal: Add Vermont + scholarly context and in-universe source handling.

### Milestone D - Consistency and Tools
Goal: Add contradiction checks and creator-facing debugging utilities.

## Prioritized Backlog

## P0

Status Summary:
- 1) Time Manager service: Completed
- 2) Letter composition UI: Completed
- 3) Prompt assembly pipeline: Completed
- 4) Story event ledger: Completed

### 1) Time Manager service
Priority: P0
Effort: M
Dependencies: none
Status: Completed
Description:
- Add a central time service for turn/date progression.
- Track player-send date and LLM-reply date (mid-month).
- Expose current timeline state to prompt and retrieval logic.
Acceptance Criteria:
- Sending a letter increments turn and advances date by one month.
- Reply date is always computed as mid-month from send month.
- Date state is exposed cleanly so a later save/load system can persist and restore it.

### 2) Letter composition UI (replace placeholder chat input)
Priority: P0
Effort: M
Dependencies: 1
Status: Completed
Description:
- Replace sample-like message UI with a letter template.
- Prefill To, From, Date, and opening/closing sections.
- Keep user focus on body content.
Acceptance Criteria:
- Player can compose only letter body in normal mode.
- Prefilled fields render correctly for each turn.
- Sent letter and received letter are viewable in sequence.

### 3) Prompt assembly pipeline
Priority: P0
Effort: M
Dependencies: 1, 2
Status: Completed
Description:
- Build runtime prompt from components:
  - role/persona
  - timeline state
  - last letter summary
  - retrieved context (when available)
- Keep prompt source modular for tuning.
Acceptance Criteria:
- Prompt output includes role framing and date context each turn.
- Prompt includes explicit chronology and 1930 cutoff constraints.
- Prompt can be inspected in debug mode.

### 4) Story event ledger
Priority: P0
Effort: M
Dependencies: 1, 3
Status: Completed
Description:
- Record canonical events by date range.
- Track generated events that occurred between letters.
- Enable retrieval and contradiction checks against ledger.
Acceptance Criteria:
- Ledger entries can be queried by date window.
- Turn output stores newly asserted events.
- Future events are not injected before valid date.

## P1

### 5) Chronology-aware RAG ingestion schema
Priority: P1
Effort: M
Dependencies: 4
Status: Completed
Description:
- Define chunk metadata:
  - sourceType (canon/local/scholarly/in-universe)
  - validFrom / validTo
  - reliability
  - tags
Acceptance Criteria:
- Ingested chunks include required metadata fields.
- Missing metadata is rejected or defaulted with warning.

### 6) Retrieval pipeline v1
Priority: P1
Effort: M
Dependencies: 5
Status: Completed
Description:
- Retrieval stages:
  - date filter
  - source-type filter
  - ranking/weighting
- Initial weighting recommendation:
  - canon > local context > scholarly > speculative
Acceptance Criteria:
- Retrieved set never contains invalid future-dated chunks.
- Retrieval returns metadata trace in debug mode.

### 7) In-universe source handling
Priority: P1
Effort: S
Dependencies: 5, 6
Status: Completed
Description:
- Support references that are fictional in reality but real in-universe.
- Tag these sources and control how confidently they are presented.
Acceptance Criteria:
- Necronomicon-style references can be retrieved when relevant.
- Output style reflects in-universe framing without modern disclaimers.

### 8) Letter archive and timeline browser
Priority: P1
Effort: S
Dependencies: 1, 2
Status: Completed
Description:
- Add read-only browser for prior sent/received letters and dates.
Acceptance Criteria:
- Player can open previous turns and inspect letter history.
- Archive order aligns with time manager turn index.

## P2

### 9) Consistency validator
Priority: P2
Effort: M
Dependencies: 4, 6
Description:
- Check draft reply assertions against ledger and chronology.
- Flag contradictions and force fallback generation path.
Acceptance Criteria:
- Contradictory outputs are detected and logged.
- Fallback output avoids introducing contradiction.

### 10) Authoring tools for content packs
Priority: P2
Effort: M
Dependencies: 5
Description:
- Build editor tooling for importing/tagging event and lore chunks.
Acceptance Criteria:
- Non-programmer workflow exists for adding/updating source material.
- Validation catches malformed dates and missing source tags.

### 11) Diagnostics panel
Priority: P2
Effort: S
Dependencies: 3, 6
Description:
- Show: built prompt sections, retrieved chunks, and timing.
Acceptance Criteria:
- One-click per-turn trace shows why output was generated.

## P3 - Enhanced UX and Presentation

### 12) Improved correspondence UI
Priority: P3
Effort: S
Dependencies: 8
Status: In Progress
Description:
- Restructure letter UI into three-column layout: Received Letter (left) | Composer (center) | Archive (right).
- Received letter displayed in prominent, readable view.
- Archive easily accessible for reference while drafting.
Acceptance Criteria:
- Player can read latest reply in large, clear view.
- Archive browser easily accessible without interrupting draft flow.
- UI responsive and balanced across three panels.

### 13) Letter drag-and-drop interactions
Priority: P3
Effort: S
Dependencies: 12
Description:
- Allow clicking and dragging letters around on-screen for organization.
- Position persists during play session.
Acceptance Criteria:
- Letters can be dragged to new positions.
- Dragging does not interfere with text selection or reading.
- Position resets on scene reload (session-only).

### 14) Multi-draft compose workflow
Priority: P3
Effort: M
Dependencies: 2
Description:
- Allow player to write multiple letter drafts and choose which to send.
- Previous drafts remain accessible for reference/revision.
Acceptance Criteria:
- Player can save draft without sending.
- Multiple drafts browsable; player selects one to send.
- Sending clears unsent drafts.

### 15) Letter composition animations
Priority: P3
Effort: M
Dependencies: 1, 2
Description:
- Animate letter text being written or typed (character-by-character).
- Later in story, some letters are typed (modern era reference).
- Optional: read-aloud narration as letter appears.
Acceptance Criteria:
- Incoming Akeley letters animate with smooth reveal.
- Animation speed configurable.
- Optional read-aloud audio tracks supported for future audio implementation.

### 16) 3D desk scene integration
Priority: P3
Effort: L
Dependencies: 2
Description:
- 3D scene showing a historical desk with letters, writing materials, lore books.
- Player composes and reads letters in immersive 3D environment.
- Option to switch between 2D UI and 3D desk view.
Acceptance Criteria:
- 3D desk scene loads and renders correctly.
- Letter interactions (read, compose, send) work in 3D context.
- Toggle between 2D and 3D modes without losing state.

### 17) Letter send/receive animations
Priority: P3
Effort: M
Dependencies: 2, 15
Description:
- Animate letter being sealed, addressed, sent (physical envelope movement).
- Animate incoming letter being delivered (physical envelope arrival).
- Support fade-in/fade-out transitions.
Acceptance Criteria:
- Outgoing letter animates through send sequence.
- Incoming letter animates through receive sequence.
- Animations skip-able with button press or auto-complete after timeout.

### 18) Akeley stability + trust model v2
Priority: P2
Effort: M
Dependencies: 3, 4
Description:
- Replace MVP keyword heuristics with richer scoring from player intent and tone.
- Track two hidden values per turn: Akeley Stability and Akeley Trust in Wilmarth.
- Record meter deltas per turn for debugging and balancing.
Acceptance Criteria:
- Meter changes are influenced by player message intent, not only keyword overlap.
- Designers can tune weights and thresholds without code changes.
- Per-turn debug trace explains why each meter changed.

### 19) Relationship-driven branching and consequences
Priority: P2
Effort: M
Dependencies: 18, 8
Description:
- Use Stability/Trust thresholds to alter letter tone, disclosures, and risk events.
- Unlock or block specific correspondence outcomes based on meter ranges.
- Surface subtle in-world feedback so players can infer relationship state.
Acceptance Criteria:
- At least three distinct narrative states are reachable from meter combinations.
- Branch outcomes are deterministic given timeline and meter state.
- Archive/history view can show key relationship-state milestones.

### 18) Endgame: Invitation to Akeley's house
Priority: P3
Effort: L
Dependencies: 1, 4, 6
Description:
- After sufficient turns and successful correspondence, Akeley invites Wilmarth to visit.
- Triggers final story sequence and gameplay arc closure.
- Option: procedural final confrontation scene based on choices made during correspondence.
Acceptance Criteria:
- Invitation appears in letter after triggering condition met.
- Final scene accessible after acceptance; shows definitive end-state.
- Player choices during correspondence influence final sequence tone/content if applicable.

## Suggested Build Order

1. 1 -> 2 -> 3
2. 4
3. 5 -> 6
4. 7 -> 8 -> 12
5. 9 -> 10 -> 11
6. 13 -> 14 -> 15 -> 16 -> 17 -> 18

## Definition of Done (Feature)

A feature is done when:
- Acceptance criteria are met.
- It respects timeline and 1930 constraints.
- It does not require modifying vendor package source.
- It has at least one validation scenario in play mode.

## Immediate Next Tickets

1. Implement consistency validator for contradiction detection and fallback generation (P2 #9).
2. Build authoring workflow for content packs with validation (P2 #10).
3. Add diagnostics panel for prompt/retrieval traces (P2 #11).

## Future Enhancement Priorities (When Ready)

- P3 #13-18: Enhanced UX with animations, 3D desk scene, drag interactions, and endgame content.
  - Recommend starting with #13 (drag) and #14 (multi-draft) as quick wins.
  - #16 (3D desk) and #18 (endgame) are major content additions requiring design review.
  - #15 (animations) and #17 (send/receive effects) enhance feel but are not critical to core loop.
