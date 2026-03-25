# P2-26 Implementation Plan - LLM Performance and Novelty Validation

## Objective
Verify that prompt updates reduce repetition and identify best model configuration on RTX 3070.

## Scope
- Baseline Qwen 3.5 2B test run.
- Escalation benchmark to Llama 3.3 8B Q5_K_M if needed.

## Work Breakdown
1. Define 5-7 turn benchmark scenario and evaluation rubric.
2. Capture novelty metrics (new development opening, repetition density).
3. Capture performance metrics (tokens/sec, VRAM peak, latency).
4. Compare model outputs with same prompts and timeline.
5. Record recommendation and operating profile in docs.

## Validation
- Quantified improvement with prompt changes is documented.
- Chosen model profile meets quality and performance targets.

## Risks
- Subjective scoring drift; use fixed rubric.
- Benchmark prompts not representative of real play.
