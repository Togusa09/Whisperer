using System.Collections.Generic;
using NUnit.Framework;
using Whisperer;

public class StoryEventMetadataValidatorTests
{
    [Test]
    public void TryNormalize_ValidEntry_ReturnsTrue()
    {
        StoryEventEntry entry = BuildValidEntry();
        List<string> messages = new List<string>();

        bool result = StoryEventMetadataValidator.TryNormalize(entry, 0, messages, out StoryEventEntry normalized);

        Assert.IsTrue(result);
        Assert.NotNull(normalized);
        Assert.AreEqual(0, messages.Count);
    }

    [Test]
    public void TryNormalize_MissingTitle_IsRejected()
    {
        StoryEventEntry entry = BuildValidEntry();
        entry.title = "";

        bool result = StoryEventMetadataValidator.TryNormalize(entry, 0, new List<string>(), out _);

        Assert.IsFalse(result);
    }

    [Test]
    public void TryNormalize_InvalidStartDate_IsRejected()
    {
        StoryEventEntry entry = BuildValidEntry();
        entry.validFromMonth = 13;

        bool result = StoryEventMetadataValidator.TryNormalize(entry, 0, new List<string>(), out _);

        Assert.IsFalse(result);
    }

    [Test]
    public void TryNormalize_MissingReliability_DefaultsBySource()
    {
        StoryEventEntry entry = BuildValidEntry();
        entry.sourceType = StoryEventMetadataValidator.SourceLocal;
        entry.reliability = 0;

        bool result = StoryEventMetadataValidator.TryNormalize(entry, 0, new List<string>(), out StoryEventEntry normalized);

        Assert.IsTrue(result);
        Assert.AreEqual(85, normalized.reliability);
    }

    [Test]
    public void TryNormalize_DeprecatedLocalContext_NormalizesToLocal()
    {
        StoryEventEntry entry = BuildValidEntry();
        entry.sourceType = "local-context";

        bool result = StoryEventMetadataValidator.TryNormalize(entry, 0, new List<string>(), out StoryEventEntry normalized);

        Assert.IsTrue(result);
        Assert.AreEqual(StoryEventMetadataValidator.SourceLocal, normalized.sourceType);
    }

    [Test]
    public void TryNormalize_TagsAreTrimmedAndDeduplicated()
    {
        StoryEventEntry entry = BuildValidEntry();
        entry.tags = new List<string> { "  hill  ", "hill", "", "  rivers" };

        bool result = StoryEventMetadataValidator.TryNormalize(entry, 0, new List<string>(), out StoryEventEntry normalized);

        Assert.IsTrue(result);
        CollectionAssert.AreEquivalent(new[] { "hill", "rivers" }, normalized.tags);
    }

    [Test]
    public void TryNormalize_EndDateEarlierThanStart_IsRejected()
    {
        StoryEventEntry entry = BuildValidEntry();
        entry.hasEndDate = true;
        entry.validToYear = 1927;
        entry.validToMonth = 12;
        entry.validToDay = 31;

        bool result = StoryEventMetadataValidator.TryNormalize(entry, 0, new List<string>(), out _);

        Assert.IsFalse(result);
    }

    static StoryEventEntry BuildValidEntry()
    {
        return new StoryEventEntry
        {
            id = "test-entry",
            title = "Test title",
            description = "Test description",
            sourceType = StoryEventMetadataValidator.SourceCanon,
            tags = new List<string> { "test" },
            validFromYear = 1928,
            validFromMonth = 5,
            validFromDay = 1,
            hasEndDate = false,
            validToYear = 0,
            validToMonth = 0,
            validToDay = 0,
            reliability = 100
        };
    }
}
