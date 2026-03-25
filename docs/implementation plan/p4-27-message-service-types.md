# P4-27 Implementation Plan - Message and Mail Service Types

## Objective
Add service-specific transit behavior (letter, night mail, train courier, telegraph).

## Scope
- Service type model and transit-time rules.
- Postmark/source metadata and ETA display.

## Work Breakdown
1. Define message service enum + transit-time policy map.
2. Extend message data model with service type, postmark office, ETA.
3. Update compose UI to choose service and preview arrival.
4. Implement in-transit queue processing by game date.
5. Update archive/history renderers for postmark and service labels.

## Validation
- Different services produce different arrival windows.
- ETA and postmark are visible and accurate.

## Risks
- Timeline math inconsistencies.
- Increased UI complexity during composition.
