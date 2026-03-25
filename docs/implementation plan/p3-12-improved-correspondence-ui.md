# P3-12 Implementation Plan - Improved Correspondence UI

## Status: Completed

## Objective
Deliver a clear two-column correspondence layout (Composer | Archive) with a dialog popup for received letters.

## Design Note
Early planning called for a persistent three-column layout. After usability review, the received-letter view was moved to a modal dialog triggered by the ComposerNotificationPanel button, keeping the two column layout uncluttered and giving the received letter full-screen reading space.

## Scope
- Two-column layout: Composer (left) | Archive (right).
- Received letter shown in a modal popup dialog when reply arrives or user reopens it.
- Composer state machine drives notification panel visibility (Compose / InTransit / ReplyReady).
- Keep draft editing uninterrupted while archive browsing.

## Work Breakdown (Completed)
1. ComposerColumn and ArchiveColumn defined in UXML.
2. LetterPopupOverlay added to UXML; wired in controller (`popupLetterContent`, `ShowLetterPopup`, `CloseLetterPopup`).
3. ComposerNotificationPanel added to UXML; state machine drives InTransit / ReplyReady label and button.
4. USS rules added for notification panel vertical layout and button sizing.
5. UpdateReceivedLetterView sets popup content on each turn completion.

## Validation
- Received letter popup opens on reply arrival.
- "Open Akeley's Reply" button reopens popup from ReplyReady state.
- Archive navigation does not clear or block current draft.
- Closing popup transitions back to Compose state.

## Risks (Resolved)
- Layout regressions in UI Toolkit style overrides — addressed by dedicated CSS classes for notification panel.
