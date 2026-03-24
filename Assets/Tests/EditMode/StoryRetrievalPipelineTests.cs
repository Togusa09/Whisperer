using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Whisperer;

public class StoryRetrievalPipelineTests
{
    [Test]
    public void Retrieve_FiltersOutFutureEvents()
    {
        StoryEventLedger ledger = CreateLedgerWithEntries(@"{
  ""entries"": [
    {
      ""id"": ""past-canon"",
      ""title"": ""Past canon"",
      ""description"": ""Past"",
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
    },
    {
      ""id"": ""future-canon"",
      ""title"": ""Future canon"",
      ""description"": ""Future"",
      ""sourceType"": ""canon"",
      ""tags"": [""canon""],
      ""validFromYear"": 1931,
      ""validFromMonth"": 1,
      ""validFromDay"": 1,
      ""hasEndDate"": false,
      ""validToYear"": 0,
      ""validToMonth"": 0,
      ""validToDay"": 0,
      ""reliability"": 100
    }
  ]
}");

        StoryRetrievalPipeline.RetrievalResult result = StoryRetrievalPipeline.Retrieve(
            ledger,
            new DateTime(1928, 5, 15),
            10,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { StoryEventMetadataValidator.SourceCanon },
            false);

        Assert.AreEqual(1, result.entries.Count);
        Assert.AreEqual("past-canon", result.entries[0].id);
        UnityEngine.Object.DestroyImmediate(ledger.gameObject);
    }

    [Test]
    public void Retrieve_RespectsSourceTypeFilter()
    {
        StoryEventLedger ledger = CreateLedgerWithEntries(@"{
  ""entries"": [
    {
      ""id"": ""canon-entry"",
      ""title"": ""Canon"",
      ""description"": ""Canon"",
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
    },
    {
      ""id"": ""local-entry"",
      ""title"": ""Local"",
      ""description"": ""Local"",
      ""sourceType"": ""local"",
      ""tags"": [""local""],
      ""validFromYear"": 1928,
      ""validFromMonth"": 5,
      ""validFromDay"": 1,
      ""hasEndDate"": false,
      ""validToYear"": 0,
      ""validToMonth"": 0,
      ""validToDay"": 0,
      ""reliability"": 85
    }
  ]
}");

        StoryRetrievalPipeline.RetrievalResult result = StoryRetrievalPipeline.Retrieve(
            ledger,
            new DateTime(1928, 5, 15),
            10,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { StoryEventMetadataValidator.SourceLocal },
            false);

        Assert.AreEqual(1, result.entries.Count);
        Assert.AreEqual("local-entry", result.entries[0].id);
        UnityEngine.Object.DestroyImmediate(ledger.gameObject);
    }

    [Test]
    public void Retrieve_PrioritizesCanonOverLocalWhenBothValid()
    {
        StoryEventLedger ledger = CreateLedgerWithEntries(@"{
  ""entries"": [
    {
      ""id"": ""local-strong"",
      ""title"": ""Local"",
      ""description"": ""Local"",
      ""sourceType"": ""local"",
      ""tags"": [""local""],
      ""validFromYear"": 1928,
      ""validFromMonth"": 5,
      ""validFromDay"": 1,
      ""hasEndDate"": false,
      ""validToYear"": 0,
      ""validToMonth"": 0,
      ""validToDay"": 0,
      ""reliability"": 100
    },
    {
      ""id"": ""canon-weak"",
      ""title"": ""Canon"",
      ""description"": ""Canon"",
      ""sourceType"": ""canon"",
      ""tags"": [""canon""],
      ""validFromYear"": 1928,
      ""validFromMonth"": 5,
      ""validFromDay"": 1,
      ""hasEndDate"": false,
      ""validToYear"": 0,
      ""validToMonth"": 0,
      ""validToDay"": 0,
      ""reliability"": 10
    }
  ]
}");

        StoryRetrievalPipeline.RetrievalResult result = StoryRetrievalPipeline.Retrieve(
            ledger,
            new DateTime(1928, 5, 15),
            1,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                StoryEventMetadataValidator.SourceCanon,
                StoryEventMetadataValidator.SourceLocal
            },
            false);

        Assert.AreEqual(1, result.entries.Count);
        Assert.AreEqual("canon-weak", result.entries[0].id);
        UnityEngine.Object.DestroyImmediate(ledger.gameObject);
    }

    [Test]
    public void Retrieve_IncludesTrace_WhenDebugEnabled()
    {
        StoryEventLedger ledger = CreateLedgerWithEntries(@"{
  ""entries"": [
    {
      ""id"": ""canon-entry"",
      ""title"": ""Canon"",
      ""description"": ""Canon"",
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

        StoryRetrievalPipeline.RetrievalResult result = StoryRetrievalPipeline.Retrieve(
            ledger,
            new DateTime(1928, 5, 15),
            10,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { StoryEventMetadataValidator.SourceCanon },
            true);

        StringAssert.Contains("Retrieval trace:", result.trace);
        StringAssert.Contains("selected entries", result.trace);
        UnityEngine.Object.DestroyImmediate(ledger.gameObject);
    }

    static StoryEventLedger CreateLedgerWithEntries(string json)
    {
        GameObject go = new GameObject("ledger-test");
        StoryEventLedger ledger = go.AddComponent<StoryEventLedger>();
        ledger.seedJson = new TextAsset(json);
        ledger.EnsureLoaded();
        return ledger;
    }
}
