using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Whisperer
{
    [Serializable]
    public class StoryEventEntry
    {
        public string id;
        public string title;
        public string description;
        public string sourceType;
        public int validFromYear;
        public int validFromMonth;
        public int validFromDay;
        public bool hasEndDate;
        public int validToYear;
        public int validToMonth;
        public int validToDay;
        public int reliability;

        public DateTime ValidFrom => new DateTime(validFromYear, validFromMonth, validFromDay);

        public bool IsActiveOn(DateTime date)
        {
            if (date < ValidFrom) return false;
            if (!hasEndDate) return true;

            DateTime validTo = new DateTime(validToYear, validToMonth, validToDay);
            return date <= validTo;
        }
    }

    [Serializable]
    public class StoryEventLedgerFile
    {
        public List<StoryEventEntry> entries = new List<StoryEventEntry>();
    }

    public class StoryEventLedger : MonoBehaviour
    {
        [Header("Seed data")]
        public TextAsset seedJson;

        [SerializeField] private List<StoryEventEntry> entries = new List<StoryEventEntry>();
        private bool loaded;

        public void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;

            if (seedJson == null || string.IsNullOrWhiteSpace(seedJson.text)) return;

            StoryEventLedgerFile parsed = JsonUtility.FromJson<StoryEventLedgerFile>(seedJson.text);
            if (parsed?.entries == null) return;
            entries = parsed.entries;
        }

        public List<StoryEventEntry> GetEventsForDate(DateTime date, int maxEntries = 6)
        {
            EnsureLoaded();
            List<StoryEventEntry> active = new List<StoryEventEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                StoryEventEntry entry = entries[i];
                if (entry.IsActiveOn(date)) active.Add(entry);
            }

            active.Sort((a, b) => b.ValidFrom.CompareTo(a.ValidFrom));
            if (active.Count > maxEntries) active.RemoveRange(maxEntries, active.Count - maxEntries);
            return active;
        }

        public string BuildContextBlock(DateTime date, int maxEntries = 6)
        {
            List<StoryEventEntry> active = GetEventsForDate(date, maxEntries);
            if (active.Count == 0) return "";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Relevant chronology and context:");
            for (int i = 0; i < active.Count; i++)
            {
                StoryEventEntry entry = active[i];
                builder.Append("- [");
                builder.Append(entry.sourceType);
                builder.Append("] ");
                builder.Append(entry.title);
                builder.Append(": ");
                builder.AppendLine(entry.description);
            }
            return builder.ToString().TrimEnd();
        }

        public void RecordGeneratedLetter(DateTime replyDate, string content)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(content)) return;

            string snippet = content.Trim();
            if (snippet.Length > 500) snippet = snippet.Substring(0, 500);

            entries.Add(new StoryEventEntry
            {
                id = $"generated-{DateTime.UtcNow.Ticks}",
                title = $"Generated reply for {replyDate:yyyy-MM}",
                description = snippet,
                sourceType = "generated-letter",
                validFromYear = replyDate.Year,
                validFromMonth = replyDate.Month,
                validFromDay = replyDate.Day,
                hasEndDate = false,
                reliability = 80
            });
        }
    }
}