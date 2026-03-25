# P4-30 Implementation Plan - Send Pictures to Player

## Objective
Allow Akeley to send visual evidence (photos/sketches/documents) as part of correspondence to improve immersion and story authenticity.

## Scope
- Support image attachments in received correspondence.
- Present attachments in UI with period-appropriate framing.
- Preserve timeline and archive consistency for media events.

## Work Breakdown
1. Extend correspondence data model with optional attachment metadata (type, title, source, date, asset reference).
2. Add UI surface in received-letter flow for viewing attached images.
3. Add archive support so historical letters can reopen their attachments.
4. Add authoring support in content pack tools to configure attachment-enabled events.
5. Add validation rules for missing assets and out-of-range timeline attachment references.

## Validation
- At least one authored event can deliver a letter with an image attachment.
- Player can open, close, and revisit the same image from archive history.
- Non-attachment letters behave unchanged.

## Risks
- Asset pipeline complexity (import, references, missing files).
- Overuse of images diluting narrative pacing.
