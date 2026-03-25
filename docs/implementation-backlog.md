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

Completed items (1-11) are tracked in: [docs/implementation-completed.md](docs/implementation-completed.md)


## Active Items

## P2

### 21) Optional: Expand historical context detection for weather queries
Priority: P2
Effort: S
Dependencies: 3, 6, 11
Status: Optional
Plan: [docs/implementation plan/p2-21-weather-history-intent.md](docs/implementation%20plan/p2-21-weather-history-intent.md)
Description:
- Expand detection of player historical-weather questions beyond month-based cues.
- Support relative time phrases such as last week, this week, previous few days, and specific day-range references.
- Improve intent parsing so weather-history context is injected only when truly relevant.
Acceptance Criteria:
- Questions like "What was the weather like last week?" and "Has this week been hot?" reliably trigger appropriate weather-history context.
- Non-weather questions do not trigger historical weather injection.
- Debug output shows detected intent and resolved time range for each historical-weather query.

## P3 - Enhanced UX and Presentation

### 12) Improved correspondence UI
Priority: P3
Effort: S
Dependencies: 8
Status: In Progress
Plan: [docs/implementation plan/p3-12-improved-correspondence-ui.md](docs/implementation%20plan/p3-12-improved-correspondence-ui.md)
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
Plan: [docs/implementation plan/p3-13-letter-drag-drop.md](docs/implementation%20plan/p3-13-letter-drag-drop.md)
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
Plan: [docs/implementation plan/p3-14-multi-draft-compose.md](docs/implementation%20plan/p3-14-multi-draft-compose.md)
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
Plan: [docs/implementation plan/p3-15-letter-composition-animations.md](docs/implementation%20plan/p3-15-letter-composition-animations.md)
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
Plan: [docs/implementation plan/p3-16-3d-desk-scene.md](docs/implementation%20plan/p3-16-3d-desk-scene.md)
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
Plan: [docs/implementation plan/p3-17-send-receive-animations.md](docs/implementation%20plan/p3-17-send-receive-animations.md)
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
Plan: [docs/implementation plan/p2-18-stability-trust-v2.md](docs/implementation%20plan/p2-18-stability-trust-v2.md)
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
Plan: [docs/implementation plan/p2-19-relationship-branching.md](docs/implementation%20plan/p2-19-relationship-branching.md)
Description:
- Use Stability/Trust thresholds to alter letter tone, disclosures, and risk events.
- Unlock or block specific correspondence outcomes based on meter ranges.
- Surface subtle in-world feedback so players can infer relationship state.
Acceptance Criteria:
- At least three distinct narrative states are reachable from meter combinations.
- Branch outcomes are deterministic given timeline and meter state.
- Archive/history view can show key relationship-state milestones.

### 20) Endgame: Invitation to Akeley's house
Priority: P3
Effort: L
Dependencies: 1, 4, 6
Plan: [docs/implementation plan/p3-20-endgame-akeley-house.md](docs/implementation%20plan/p3-20-endgame-akeley-house.md)
Description:
- After sufficient turns and successful correspondence, Akeley invites Wilmarth to visit.
- Triggers final story sequence and gameplay arc closure.
- Option: procedural final confrontation scene based on choices made during correspondence.
Acceptance Criteria:
- Invitation appears in letter after triggering condition met.
- Final scene accessible after acceptance; shows definitive end-state.
- Player choices during correspondence influence final sequence tone/content if applicable.

## Suggested Build Order

1. 12 -> 13
2. 30 (optional, after drag interactions)
3. 14 -> 15 -> 17
4. 16
5. 18 -> 19 -> 20
6. 21 (optional) -> 26
7. 27 -> 28 -> 29

## Definition of Done (Feature)

A feature is done when:
- Acceptance criteria are met.
- It respects timeline and 1930 constraints.
- It does not require modifying vendor package source.
- It has at least one validation scenario in play mode.

## Immediate Next Tickets

1. Extend weather-history intent parsing to support weekly and relative-range phrasing (P2 #21 optional).
2. Continue improved correspondence UI work (P3 #12).
3. Evaluate whether letter drag-and-drop interactions (P3 #13) or multi-draft workflow (P3 #14) is the better next UX slice.

## P2 (Narrative & Model Tuning)

### 26) LLM Model Performance & Novelty Validation
Priority: P2
Effort: L (time-permitting)
Dependencies: #10 (Content Pack Authoring complete), improved LetterPromptBuilder (implemented Mar 2026)
Status: Planned
Plan: [docs/implementation plan/p2-26-llm-performance-novelty-validation.md](docs/implementation%20plan/p2-26-llm-performance-novelty-validation.md)
Description:
- Validate that improved LetterPromptBuilder anti-repetition hard rules reduce redundant content generation.
- Test suite: Run 5–7 turns with Qwen 3.5 2B to confirm novelty gains from prompt changes alone.
- If repetition persists, benchmark upgrade to Llama 3.3 8B (Q5_K_M GGUF) on RTX 3070.
- Measure and document: tokens/sec, VRAM peak, response novelty score (binary: "new idea" vs. "rehashed").
- Optional: After validation, evaluate Nous Hermes 3 as a secondary "polish pass" for deeper Akeley voice.

Acceptance Criteria:
- After 5+ turns with improved prompt, Akeley's replies show clear progression (new discoveries/observations each turn, no full-sentence repetition).
- If upgraded to Llama 3.3 8B, the same 5-turn test shows <2% repeated vocabulary density and consistent constraint-following.
- VRAM usage documented (target: <5GB peak on RTX 3070).
- Model swap can be performed in LLMUnity without code changes (GGUF loading).
- Results recorded in git commit or doc reference for future tuning.

Notes:
- RTX 3070 headroom is sufficient: Q5_K_M 8B uses ~4.8GB, leaving ~3.2GB for Unity.
- Qwen 2.5 7B is an alternative mid-tier option if Llama 3.3 unavailable.
- See [docs/llm-model-selection.md](docs/llm-model-selection.md) for full selection logic and hardware specs.

---

## Future Enhancement Priorities (When Ready)

- P3 #13-18: Enhanced UX with animations, 3D desk scene, drag interactions, and endgame content.
  - Recommend starting with #13 (drag) and #14 (multi-draft) as quick wins.
  - #16 (3D desk) and #18 (endgame) are major content additions requiring design review.
  - #15 (animations) and #17 (send/receive effects) enhance feel but are not critical to core loop.

---

## P4 (Advanced Narrative Features & Message System)

### 27) Message and mail service types (transit system)
Priority: P4
Effort: L
Dependencies: 1 (Time Manager), 2 (Letter UI)
Status: Planned
Plan: [docs/implementation plan/p4-27-message-service-types.md](docs/implementation%20plan/p4-27-message-service-types.md)
Description:
- Expand correspondence beyond simple monthly letters to include multiple message types:
  - Letters (primary, current turnaround about one month)
  - Night mail (faster, about one week turnaround)
  - Courier by train (medium speed, about two weeks)
  - Telegraph (instant or near-instant)
- Each service type has different transit times and allows asynchronous messaging.
- Add visual postmark indicators showing sending office and service type.
- UI displays expected arrival date for outgoing messages.
Acceptance Criteria:
- At least two message types (letters plus one faster service) can be composed and sent.
- Transit times are mathematically consistent with game calendar progression.
- Postmarks render correctly in archive/history surfaces.
- Player can see expected arrival date before committing to send.
Notes:
- Enables scenarios where a telegraph arrives before a letter, or both sides have letters in transit at once.
- Asynchronous handling requires explicit in-transit message state tracking.

### 28) Fake and missing correspondence mechanics
Priority: P4
Effort: M
Dependencies: 1, 27, 4 (Story Event Ledger)
Status: Planned
Plan: [docs/implementation plan/p4-28-fake-missing-correspondence.md](docs/implementation%20plan/p4-28-fake-missing-correspondence.md)
Description:
- Fake correspondence: player receives mail that the LLM did not send and is not aware of sending.
- Missing correspondence: LLM sends a message that the player never receives and therefore cannot directly reply to.
- Both mechanics are recorded in ledger/history with authoritative state to preserve chronology and consistency.
Acceptance Criteria:
- Fake mail can be injected through an editor/debug path for author testing.
- Missing mail can be triggered by narrative conditions and service/transit rules.
- Ledger tracks message state for received, sent-but-missing, and fake entries.
- Archive/history can optionally reveal true message state after configured story conditions.
Notes:
- Designed to increase narrative uncertainty without breaking timeline consistency.
- Should be gated by authored rules to avoid player confusion or perceived unfairness.

### 29) LLM generation-time masking for asynchronous messaging
Priority: P4
Effort: M
Dependencies: 27, 28, 2 (Letter UI)
Status: Planned
Plan: [docs/implementation plan/p4-29-llm-generation-time-masking.md](docs/implementation%20plan/p4-29-llm-generation-time-masking.md)
Description:
- Current synchronous flow exposes obvious "LLM is generating" waits.
- For asynchronous and missing-message gameplay, conceal generation timing behind in-world delivery behavior.
- Support scheduling, delayed arrival presentation, and background generation so visible UI is framed as transit and delivery, not model work.
- Preserve player agency to pause and think before composing without causing timeline or immersion issues.
Acceptance Criteria:
- Missing messages do not surface raw generation/loading indicators to player-facing UI.
- Scheduled asynchronous arrivals trigger at expected game-time windows with delivery notifications.
- Player can pause composition while time perspective remains coherent and in-world.
- Core monthly turn still supports immediate wait-for-reply flow when desired.
Notes:
- This is primarily a UX and immersion layer over async message orchestration.
- Coordinate with animation/sound delivery cues for believable mailbox and telegraph arrivals.

### 30) Optional: Allow sending pictures to player
Priority: P4
Effort: M
Dependencies: 2, 8, 10, 13
Status: Optional
Plan: [docs/implementation plan/p4-30-send-pictures-to-player.md](docs/implementation%20plan/p4-30-send-pictures-to-player.md)
Description:
- Support image attachments in received correspondence when story events require visual evidence.
- Attachments can represent photographs, sketches, clippings, or documents that Akeley sends.
- Keep this optional so narrative pacing remains letter-first by default.
Acceptance Criteria:
- At least one authored correspondence event can include a picture attachment.
- Player can open attachment from the received-letter flow and from archive history.
- Letters without attachments behave exactly as before.
Notes:
- This feature is primarily for immersion and should be used selectively.
- Attachments should remain timeline-consistent and period-appropriate.
