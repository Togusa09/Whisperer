# AGENTS.md

## Repository editing policy

- Treat the `ai.undream.llm` Unity package as third-party vendor content.
- `Assets/_Reference/LLMUnitySamples/` contains copied example code and must be considered reference-only.
- Do not edit third-party package source or files under `Assets/_Reference/LLMUnitySamples/**` unless the user explicitly asks for a change there.
- Implement project changes in first-party code paths (for example `Assets/_Game/`) whenever possible.
- If a requested feature appears to require changing vendor code, ask for explicit confirmation before editing it.

## Narrative behavior policy for AI assistants

- For story and character behavior, follow `Assets/_Game/README.md`.
- Treat `Assets/_Game/README.md` as the source of truth for:
	- Henry W. Akeley / Albert N. Wilmarth role framing.
	- Monthly letter cadence.
	- Historical and chronology constraints.
	- 1930 knowledge cutoff and period-appropriate speculation limits.
