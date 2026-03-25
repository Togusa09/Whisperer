# P2-21 Implementation Plan - Weather History Intent Expansion

## Objective
Expand historical weather intent detection to support relative time phrases while avoiding false positives.

## Scope
- Parse phrases like "last week", "this week", "previous few days", and explicit day ranges.
- Resolve each phrase to a concrete date window using game timeline dates.
- Inject weather-history context only for weather-related intents.

## Work Breakdown
1. Add a phrase parser in the weather-intent path (normalization + pattern matching).
2. Convert parsed phrases to date ranges relative to reply context date.
3. Gate injection behind weather intent confidence thresholds.
4. Add diagnostics output for intent + resolved range.
5. Add tests for positive and negative prompts.

## Validation
- "What was the weather like last week?" injects historical context with correct date window.
- Non-weather prompts with date words do not inject historical weather context.

## Risks
- Over-triggering on generic time words.
- Off-by-one date windows around month boundaries.
