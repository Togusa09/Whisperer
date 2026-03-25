# P2 #10 Planning: Authoring Tools for Content Packs

## Objective
Enable non-programmer workflows for adding and maintaining chronology RAG content with schema validation and clear diagnostics.

## Proposed Scope
- Editor window for story-event authoring and import.
- JSON schema-guided form editing for `StoryEventEntry` fields.
- Validation pass using existing `StoryEventMetadataValidator` before save.
- Batch import pathway for CSV/JSON content packs with preview and warnings.

## Milestone Plan

### M1: Authoring Surface (Editor)
- Add `Window/Whisperer/Content Pack Authoring` editor window.
- Provide create/edit/delete for entries with fields:
  - `id`, `title`, `description`, `sourceType`, `tags`
  - `validFrom`, `validTo`, `reliability`
- Add metadata help text for source-type semantics.

### M2: Validation + Save Flow
- Integrate `StoryEventMetadataValidator.TryNormalize` on each draft entry.
- Display inline errors/warnings before save.
- Save to `Assets/_Game/Resources/Whisperer/story-events.json` with deterministic ordering.
- Restrict `sourceType` to a dropdown of supported values so invalid types cannot be entered manually.

### M3: Import/Export Workflow
- Import JSON file and show diff preview against current ledger.
- Optional CSV import template mapped to `StoryEventEntry`.
- Export current ledger to timestamped backup.

### M4: Diagnostics Integration
- Hook into existing diagnostics to show last authoring validation summary.
- Add one-click "Open content source" from diagnostics editor window.

## Acceptance Mapping

### AC1: Non-programmer workflow exists
- Addressed by M1 + M2 editor UI and guided fields.

### AC2: Validation catches malformed dates and missing source tags
- Addressed by M2 pre-save validation and warning display.

## Technical Notes
- Reuse existing models in:
  - `Assets/_Game/Scripts/Core/StoryEventLedger.cs`
  - `Assets/_Game/Scripts/Core/StoryEventMetadataValidator.cs`
- Avoid changes to vendor package code.
- Keep JSON format backwards compatible with current runtime loader.

## Initial Task Breakdown
1. Create editor window scaffold and load current ledger JSON.
2. Implement entry list + detail inspector UI.
3. Wire normalization/validation in edit and save paths.
4. Add import preview and merge strategy (append/replace/update-by-id).
5. Add edit-mode tests for serializer ordering and validation behavior.
