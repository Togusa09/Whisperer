using System;
using NUnit.Framework;
using UnityEngine;
using Whisperer;

public class LetterPromptBuilderTests
{
    [Test]
    public void BuildSystemPrompt_IncludesDateAndRulesAndContext()
    {
        GameObject go = new GameObject("prompt-builder-test");
        GameTimeManager time = go.AddComponent<GameTimeManager>();
        StoryEventLedger ledger = go.AddComponent<StoryEventLedger>();
        LetterPromptBuilder builder = go.AddComponent<LetterPromptBuilder>();

        ledger.seedJson = new TextAsset(@"{
  ""entries"": [
    {
      ""id"": ""canon-entry"",
      ""title"": ""Canon"",
      ""description"": ""Canon context"",
      ""sourceType"": ""canon"",
      ""tags"": [""canon""],
      ""validFromYear"": 1928,
      ""validFromMonth"": 5,
      ""validFromDay"": 1,
      ""hasEndDate"": false,
      ""validToYear"": 0,
      ""validToMonth"": 0,
      ""validToDay"": 0,
      ""reliability"": 100
    }
  ]
}");

        ledger.EnsureLoaded();
        builder.storyEventLedger = ledger;
        builder.debugLogPrompts = true;

        string prompt = builder.BuildSystemPrompt(time, "Prior letter summary text");

        StringAssert.Contains("Hard rules:", prompt);
        StringAssert.Contains("Player send date:", prompt);
        StringAssert.Contains("Relevant chronology and context:", prompt);
        StringAssert.Contains("Canon context", prompt);
        StringAssert.Contains("previous letter summary", prompt.ToLowerInvariant());
        StringAssert.Contains("Retrieval trace:", builder.LastRetrievalTrace);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void BuildUserTurnPrompt_FormatsLetterHeader()
    {
        GameObject go = new GameObject("prompt-builder-test");
        GameTimeManager time = go.AddComponent<GameTimeManager>();
        LetterPromptBuilder builder = go.AddComponent<LetterPromptBuilder>();

        string userPrompt = builder.BuildUserTurnPrompt(time, "My dear Mr. Akeley,\n\nTest body.");

        StringAssert.Contains("Letter from Albert N. Wilmarth dated", userPrompt);
        StringAssert.Contains("Reply as Henry W. Akeley in letter form.", userPrompt);
        StringAssert.Contains("Test body.", userPrompt);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void BuildSystemPrompt_IncludesInUniverseFraming_WhenRetrieved()
    {
        GameObject go = new GameObject("prompt-builder-in-universe-test");
        GameTimeManager time = go.AddComponent<GameTimeManager>();
        StoryEventLedger ledger = go.AddComponent<StoryEventLedger>();
        LetterPromptBuilder builder = go.AddComponent<LetterPromptBuilder>();

        ledger.seedJson = new TextAsset(@"{
  ""entries"": [
    {
      ""id"": ""in-universe-1"",
      ""title"": ""Occult report"",
      ""description"": ""Whispered testimony attributed to a forbidden grimoire."",
      ""sourceType"": ""in-universe"",
      ""tags"": [""necronomicon""],
      ""validFromYear"": 1928,
      ""validFromMonth"": 1,
      ""validFromDay"": 1,
      ""hasEndDate"": false,
      ""validToYear"": 0,
      ""validToMonth"": 0,
      ""validToDay"": 0,
      ""reliability"": 52
    }
  ]
}");

        ledger.EnsureLoaded();
        builder.storyEventLedger = ledger;

        string prompt = builder.BuildSystemPrompt(time, "");

        StringAssert.Contains("In-universe source handling:", prompt);
        StringAssert.Contains("Do not use modern out-of-universe disclaimers.", prompt);
        StringAssert.Contains("tentative confidence", prompt);
        UnityEngine.Object.DestroyImmediate(go);
    }
}
