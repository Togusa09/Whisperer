using System;
using System.Collections.Generic;
using System.Text;

namespace Whisperer
{
    public static class StoryRetrievalPipeline
    {
        public readonly struct RetrievalResult
        {
            public readonly List<StoryEventEntry> entries;
            public readonly string trace;

            public RetrievalResult(List<StoryEventEntry> entries, string trace)
            {
                this.entries = entries;
                this.trace = trace;
            }
        }

        public static RetrievalResult Retrieve(
            StoryEventLedger ledger,
            DateTime timelineDate,
            int maxEntries,
            HashSet<string> allowedSourceTypes,
            bool includeDebugTrace)
        {
            if (ledger == null)
            {
                return new RetrievalResult(new List<StoryEventEntry>(), includeDebugTrace ? "Retrieval skipped: ledger not provided." : "");
            }

            List<StoryEventEntry> candidates = ledger.GetEntriesSnapshot();
            List<ScoredEntry> scored = new List<ScoredEntry>();
            int filteredOutByDate = 0;
            int filteredOutBySource = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                StoryEventEntry entry = candidates[i];
                if (!entry.IsActiveOn(timelineDate))
                {
                    filteredOutByDate++;
                    continue;
                }

                if (allowedSourceTypes != null && allowedSourceTypes.Count > 0 && !allowedSourceTypes.Contains(entry.sourceType))
                {
                    filteredOutBySource++;
                    continue;
                }

                int score = ScoreEntry(entry, timelineDate);
                scored.Add(new ScoredEntry(entry, score));
            }

            scored.Sort((a, b) => b.score.CompareTo(a.score));

            List<StoryEventEntry> selected = new List<StoryEventEntry>();
            int takeCount = Math.Min(Math.Max(1, maxEntries), scored.Count);
            for (int i = 0; i < takeCount; i++)
            {
                selected.Add(scored[i].entry);
            }

            string trace = "";
            if (includeDebugTrace)
            {
                trace = BuildTrace(timelineDate, candidates.Count, filteredOutByDate, filteredOutBySource, scored, selected);
            }

            return new RetrievalResult(selected, trace);
        }

        public static string BuildContextBlock(List<StoryEventEntry> entries)
        {
            if (entries == null || entries.Count == 0) return "";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Relevant chronology and context:");
            for (int i = 0; i < entries.Count; i++)
            {
                StoryEventEntry entry = entries[i];
                builder.Append("- [");
                builder.Append(entry.sourceType);
                builder.Append("] ");
                builder.Append(entry.title);
                builder.Append(": ");
                builder.AppendLine(entry.description);
            }

            return builder.ToString().TrimEnd();
        }

        static int ScoreEntry(StoryEventEntry entry, DateTime timelineDate)
        {
            int sourceWeight = GetSourceWeight(entry.sourceType);
            int reliability = Math.Clamp(entry.reliability, 0, 100);

            int recencyDays = 0;
            if (entry.ValidFrom <= timelineDate)
            {
                recencyDays = (timelineDate - entry.ValidFrom).Days;
            }

            int recencyBonus = Math.Max(0, 3650 - recencyDays); // prefer more recent context within a 10-year window
            return sourceWeight * 10000 + reliability * 100 + recencyBonus;
        }

        static int GetSourceWeight(string sourceType)
        {
            switch (sourceType)
            {
                case StoryEventMetadataValidator.SourceCanon:
                    return 100;
                case StoryEventMetadataValidator.SourceLocal:
                    return 80;
                case StoryEventMetadataValidator.SourceScholarly:
                    return 60;
                case StoryEventMetadataValidator.SourceSpeculative:
                    return 40;
                case StoryEventMetadataValidator.SourceInUniverse:
                    return 50;
                case StoryEventMetadataValidator.SourceGeneratedLetter:
                    return 70;
                default:
                    return 10;
            }
        }

        static string BuildTrace(
            DateTime timelineDate,
            int candidateCount,
            int filteredOutByDate,
            int filteredOutBySource,
            List<ScoredEntry> scored,
            List<StoryEventEntry> selected)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Retrieval trace:");
            builder.Append("- timeline date: ");
            builder.AppendLine(timelineDate.ToString("yyyy-MM-dd"));
            builder.Append("- candidates loaded: ");
            builder.AppendLine(candidateCount.ToString());
            builder.Append("- filtered by date: ");
            builder.AppendLine(filteredOutByDate.ToString());
            builder.Append("- filtered by source: ");
            builder.AppendLine(filteredOutBySource.ToString());
            builder.Append("- ranked candidates: ");
            builder.AppendLine(scored.Count.ToString());
            builder.Append("- selected entries: ");
            builder.AppendLine(selected.Count.ToString());

            for (int i = 0; i < scored.Count && i < 12; i++)
            {
                ScoredEntry scoredEntry = scored[i];
                bool chosen = i < selected.Count;
                builder.Append(chosen ? "  * " : "  - ");
                builder.Append(scoredEntry.entry.id);
                builder.Append(" | ");
                builder.Append(scoredEntry.entry.sourceType);
                builder.Append(" | score=");
                builder.Append(scoredEntry.score);
                builder.Append(" | reliability=");
                builder.Append(scoredEntry.entry.reliability);
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        readonly struct ScoredEntry
        {
            public readonly StoryEventEntry entry;
            public readonly int score;

            public ScoredEntry(StoryEventEntry entry, int score)
            {
                this.entry = entry;
                this.score = score;
            }
        }
    }
}