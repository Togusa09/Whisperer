# Project Setup Notes

## LLMUnity Package Source

LLMUnity is installed via Unity Package Manager from GitHub:

- `ai.undream.llm`: `https://github.com/undreamai/LLMUnity.git#v3.0.3`

On first open, Unity resolves this dependency automatically.

## Reference Samples

LLMUnity examples are kept for reference at:

- `Assets/_Reference/LLMUnitySamples/`

They are reference-only and should not be modified unless explicitly requested.

## LLMUnity Runtime Binaries

This project does not track the large LlamaLib runtime binaries in source
control.

- Ignored path pattern: `Assets/StreamingAssets/LlamaLib-v*/`
- On first open (with internet access), LLMUnity downloads the required
  LlamaLib runtime automatically.

If the runtime is missing or corrupted, use the LLMUnity setup controls in the
Unity editor to re-download the library.