# Whisperer

Whisperer is a Unity narrative prototype inspired by H. P. Lovecraft's *The Whisperer in Darkness*.

You play as Albert N. Wilmarth and write monthly letters. The model replies in-character as Henry W. Akeley, with timeline-aware constraints and an in-universe tone.

## Current MVP

- Letter-based core loop (send monthly letter, receive Akeley's reply)
- Time system with monthly turn progression and mid-month reply context
- Prompt pipeline with historical and chronology constraints
- Story event ledger and retrieval-aware context framing
- Archive browser for reviewing prior correspondence
- Relationship MVP meters:
  - Akeley Stability (0-100)
  - Akeley Trust in Wilmarth (0-100)

## Tech Stack

- Unity project
- LLMUnity (`ai.undream.llm`)
- Unity MCP tooling (`com.ivanmurzak.unity.mcp`)

## Quick Start

1. Open the project in Unity.
2. Allow Package Manager to resolve dependencies.
3. Configure LLMUnity model/runtime in-editor.
4. Enter Play Mode and use the correspondence UI.

## Setup Notes

For environment and dependency details, see:

- [SETUP.md](SETUP.md)
- [docs/unity-mcp.md](docs/unity-mcp.md)

## Gameplay Prompting Rules

Narrative and role constraints are documented here:

- [Assets/_Game/README.md](Assets/_Game/README.md)

Key rules include:

- Epistolary style only (no modern chat style)
- Timeline consistency per turn
- No real-world knowledge after 1930

## Repository Editing Policy

Before changing code, review:

- [AGENTS.md](AGENTS.md)

Important:

- Treat `ai.undream.llm` as third-party package code.
- Treat `Assets/_Reference/LLMUnitySamples/` as reference-only.
- Prefer implementing changes in first-party paths such as `Assets/_Game/`.

## Project Structure

- `Assets/_Game/`: first-party game logic, UI, prompts, and data
- `Assets/_Reference/`: reference sample content (do not edit unless requested)
- `docs/`: backlog and implementation docs

## Backlog

Planned work and milestones:

- [docs/implementation-backlog.md](docs/implementation-backlog.md)

Notable upcoming items include expanded Stability/Trust modeling and relationship-driven narrative branching.

## Testing And Validation

- Use Unity EditMode/PlayMode tests where available.
- Prompt and runtime diagnostics can be enabled in prompt-related components for debugging.
