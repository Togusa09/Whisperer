# LLM Model Selection & Configuration

## Executive Summary

**Your Setup:**
- GPU: RTX 3070 (8GB VRAM)
- Framework: LLMUnity (llama.cpp wrapper)
- Current Model: Qwen 3.5 2B
- **Status:** Improved prompt constraints deployed; ready for model evaluation and upgrade planning.

---

## Current State: Prompt Improvements (March 2026)

The `LetterPromptBuilder` has been updated with three explicit anti-repetition hard rules:
- "Do not repeat, paraphrase, or summarise sentences from your previous letter."
- "Do not restate things Akeley has already told Wilmarth."
- "Each letter must advance the story: report something new Akeley has observed, discovered, or decided to disclose for the first time."

**Plus:** Strengthened response format instructs the model to "open with one new concrete development" and relegates historical details to brief context only.

**Impact:** This significantly improves output novelty at any model size, but especially noticeable on smaller models (2B–7B). Consider re-testing Qwen 2.5 2B with the improved prompt before upgrading hardware.

---

## Hardware Headroom

| Model | Quantization | VRAM Used | VRAM Free | Notes |
|-------|--------------|-----------|-----------|-------|
| Qwen 3.5 2B | Q5_K_M | ~1.5GB | 6.5GB | Current; plenty of headroom |
| Llama 3.3 8B | Q5_K_M | ~4.8GB | 3.2GB | **Recommended upgrade** |
| Nous Hermes 3 8B | Q5_K_M | ~4.8GB | 3.2GB | Secondary option |
| Qwen 2.5 7B | Q5_K_M | ~4.2GB | 3.8GB | Alternative mid-tier |

**RTX 3070 is sufficiently powered for Q5_K_M 8B models.** The 3.2GB free headroom is adequate for Unity runtime + OS.

---

## Recommended Model Upgrade Path

### Phase 1: Validate Improved Prompt (Current)
- **Model:** Qwen 3.5 2B (no new hardware required)
- **Goal:** Confirm anti-repetition rules work at small scale.
- **Success Metric:** After 5 turns, no repeated sentences; each reply opens with something new.
- **Next Step:** If still seeing repetition, proceed to Phase 2. If resolved, stop here.

### Phase 2: Upgrade to Llama 3.3 8B Instruct (Recommended First 8B)
- **Why:** Superior constraint-following and instruction adherence. The updated prompt's explicit hard rules pair perfectly with Llama 3.3's deterministic rule-following.
- **Format:** GGUF, Q5_K_M quantization (~4.8GB VRAM).
- **Expected Performance:** 2–4 second inference per letter on RTX 3070.
- **Inference Speed:** ~50–100 tokens/sec (typical for 8B on 3070).
- **Character Voice:** Slightly more "clinical" than Hermes, but more predictable for state-driven narratives.
- **Where to Find:** HuggingFace `meta-llama/Llama-2-3.3-8B-Instruct` or compatible GGUF conversion.

### Phase 3: Polish with Nous Hermes 3 (Optional, After Validation)
- **When:** Only after narrative beats and prompt constraints are proven solid.
- **Why:** If Llama 3.3 feels too rigid, Hermes excels at deeper character voice and long-form flourish.
- **Format:** GGUF, Q5_K_M (~4.8GB VRAM).
- **Use Case:** Final "pass" for Akeley's Lovecraftian tone and epistolary richness.

---

## Configuration Best Practices for LLMUnity

### Context Window
```
n_ctx = 8192
```
Lovecraftian letters can grow long. With retrieval context + prior correspondence, 8K buffer is necessary.

### Quantization Notes
- **Q5_K_M:** Recommended. Near-lossless quality, good compression.
- **Q4_K_M:** If you need more VRAM headroom (e.g., for larger models in future). Quality degradation is noticeable for creative writing.
- **Q6_K:** If you upgrade GPU or use smaller models. Maximal quality, ~8GB for 8B model.

### Flash Attention
- **Enable if using CUDA.** Check LLMUnity GPU settings; this speeds up inference significantly on RTX 3070.
- Reduces VRAM pressure, improves throughput.

### Batch Size
- Start with batch_size = 1 for real-time gameplay.
- Monitor VRAM peak during early turns; if headroom exists, can increase.

---

## Model Comparison: Detailed

### Llama 3.3 8B Instruct (RECOMMENDED)
**Pros:**
- Excellent instruction-following. Respects "do not repeat" constraints reliably.
- Deterministic and predictable. Great for rule-bound game logic.
- Stable across 5+ turns without drift.
- Community-wide adoption; plenty of GGUF variants available.

**Cons:**
- Can feel slightly "robotic" compared to Hermes.
- May require more explicit framing in system prompt to achieve Akeley's literary tone.

**Verdict:** Best first upgrade for constraint-heavy epistolary narratives.

---

### Nous Hermes 3 (Llama 3.2 8B)
**Pros:**
- Exceptional long-form narrative and character consistency.
- Naturally maintains academic/historical voice (Akeley's style).
- Creative while still respecting constraints (better than base Llama).
- Slightly smaller than Llama 3.3, slightly faster inference.

**Cons:**
- Can hallucinate or ignore constraints if not carefully prompted.
- "Too creative" can lead to contradictions with your story database.
- Requires more robust system prompt to enforce hard rules.

**Verdict:** Use *after* Llama 3.3 validation, as a secondary "polish" option.

---

### Qwen 2.5 7B (Alternative Mid-Tier)
**Pros:**
- Balanced. Better instruction-following than Qwen 3.5 2B, but not 8B-heavyweight.
- Reasonable narrative quality; decent for academic tone.
- Smaller than 8B; slightly less VRAM (~4.2GB Q5_K_M).

**Cons:**
- Not as strong as Llama 3.3 or Hermes at either constraint-following or creative narrative.
- Fewer GGUF variants available.

**Verdict:** Consider only if Llama 3.3 is unavailable or you want a stepping stone between 2B and 8B.

---

## Sentis vs. LLMUnity (Clarification)

- **LLMUnity:** Full GGUF ecosystem. Use this for 8B models like Llama 3.3 and Hermes.
- **Sentis:** Lightweight inference, small models (100M–1B). Not suitable for your use case.

**You are on the correct path with LLMUnity.**

---

## Next Steps

1. **Test improved Qwen 2.5 2B prompt** with the updated `LetterPromptBuilder`:
   - Run 5+ turns and evaluate for repetition.
   - If acceptable, document results.

2. **If repetition persists, download Llama 3.3 8B Instruct (Q5_K_M GGUF)**:
   - Sources: HuggingFace, TheBloke's quantizations, or similar.
   - Load into LLMUnity and run same 5-turn test.

3. **Measure and log:**
   - Tokens/sec inference speed
   - VRAM peak usage
   - Response novelty (binary: "new idea opened letter" or "rehashed content")

4. **Document results** in a git commit or session note for reference.

---

## References

- **LLMUnity Docs:** [Link to repo or docs]
- **GGUF Model Sources:** HuggingFace (search "Llama 3.3 8B GGUF"), TheBloke's quantizations
- **LetterPromptBuilder (Updated):** [Assets/_Game/Scripts/Prompting/LetterPromptBuilder.cs](Assets/_Game/Scripts/Prompting/LetterPromptBuilder.cs)
- **Improved Prompt Constraints:** Hard rules added for anti-repetition; response format emphasizes novelty
