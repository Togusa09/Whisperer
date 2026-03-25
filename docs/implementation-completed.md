# Whisperer Completed Backlog Items

This document tracks completed items moved out of the active backlog.

## P0 Completed

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

## P1 Completed

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

## P2 Completed

### 9) Consistency validator
Priority: P2
Effort: M
Dependencies: 4, 6
Status: Completed
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
Status: Completed
Description:
- Build editor tooling for importing/tagging event and lore chunks.
Acceptance Criteria:
- Non-programmer workflow exists for adding/updating source material.
- Validation catches malformed dates and missing source tags.

### 11) Diagnostics panel
Priority: P2
Effort: S
Dependencies: 3, 6
Status: Completed
Description:
- Show: built prompt sections, retrieved chunks, and timing.
Acceptance Criteria:
- One-click per-turn trace shows why output was generated.
