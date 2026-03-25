# P3-14 Implementation Plan - Multi-Draft Compose Workflow

## Objective
Support multiple unsent drafts and explicit draft selection before sending.

## Scope
- Draft list, create/save/delete/select actions.
- Single send action for currently selected draft.

## Work Breakdown
1. Define draft data model and storage lifecycle.
2. Add draft sidebar/list UI with active selection.
3. Implement save/update for current editor contents.
4. Implement send action that clears unsent drafts per backlog requirement.
5. Add tests around switching drafts and send semantics.

## Validation
- Player can create and switch among multiple drafts.
- Sending chosen draft clears remaining unsent drafts.

## Risks
- Accidental data loss on draft switching.
- Confusion between archive entries and unsent drafts.
