# AGENTS.md

## Repository editing policy

- Treat the `ai.undream.llm` Unity package as third-party vendor content.
- `Assets/_Reference/LLMUnitySamples/` contains copied example code and must be considered reference-only.
- Do not edit third-party package source or files under `Assets/_Reference/LLMUnitySamples/**` unless the user explicitly asks for a change there.
- Implement project changes in first-party code paths (for example `Assets/_Game/`) whenever possible.
- If a requested feature appears to require changing vendor code, ask for explicit confirmation before editing it.
- Treat `.github/skills/**` as Unity MCP generated artifacts; prefer regeneration over large manual rewrites.

## Narrative behavior policy for AI assistants

- For story and character behavior, follow `Assets/_Game/README.md`.
- Treat `Assets/_Game/README.md` as the source of truth for:
	- Henry W. Akeley / Albert N. Wilmarth role framing.
	- Monthly letter cadence.
	- Historical and chronology constraints.
	- 1930 knowledge cutoff and period-appropriate speculation limits.

## Test execution policy for AI agents

- Do not use `dotnet test` to validate Unity tests in this repository.
- Unity-generated test projects compile under `dotnet test`, but the Unity Test Runner tests are not executed there.
- Use `dotnet build Whisperer.slnx -nologo` for compile-only verification.
- Use Unity Test Runner (batch mode) for actual test execution.