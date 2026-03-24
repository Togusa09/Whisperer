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

### 1) Time Manager service
Priority: P0
Effort: M
Dependencies: none
Description:
- Add a central time service for turn/date progression.
- Track player-send date and LLM-reply date (mid-month).
- Expose current timeline state to prompt and retrieval logic.
Acceptance Criteria:
- Sending a letter increments turn and advances date by one month.
- Reply date is always computed as mid-month from send month.
- Date state persists through save/load.

### 2) Letter composition UI (replace placeholder chat input)
Priority: P0
Effort: M
Dependencies: 1
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

## Suggested Build Order

1. 1 -> 2 -> 3
2. 4
3. 5 -> 6
4. 7 -> 8
5. 9 -> 10 -> 11

## Definition of Done (Feature)

A feature is done when:
- Acceptance criteria are met.
- It respects timeline and 1930 constraints.
- It does not require modifying vendor package source.
- It has at least one validation scenario in play mode.

## Immediate Next Tickets

1. Implement Time Manager script and persistence model.
2. Replace current input area with letter template UI prefab.
3. Add prompt builder service with debug preview toggle.
4. Create initial event ledger JSON and loader.
5. Build minimal date-filtered retrieval over tagged chunks.
