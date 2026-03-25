using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Whisperer
{
    public class StoryConsistencyValidator : MonoBehaviour
    {
        [Header("Debug")]
        public bool debugLogValidation = true;
        [TextArea(4, 20)]
        [SerializeField] string lastReport = "";

        static readonly Regex YearRegex = new Regex(@"\b(1[89]\d{2}|20\d{2})\b", RegexOptions.Compiled);
        static readonly Regex MonthYearRegex = new Regex(@"\b(January|February|March|April|May|June|July|August|September|October|November|December)\s+(1[89]\d{2}|20\d{2})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string LastReport => lastReport;

        public ValidationResult ValidateDraft(DateTime replyDate, int knowledgeCutoffYear, string draft, StoryEventLedger ledger)
        {
            List<string> issues = new List<string>();

            if (string.IsNullOrWhiteSpace(draft))
            {
                issues.Add("Draft was empty.");
                return BuildResult(false, issues);
            }

            ValidateYearConstraints(replyDate, knowledgeCutoffYear, draft, issues);
            ValidateFutureMonthReferences(replyDate, draft, issues);
            ValidateFutureLedgerLeakage(replyDate, draft, ledger, issues);
            ValidateGeneratedLetterContradictions(replyDate, draft, ledger, issues);

            bool consistent = issues.Count == 0;
            return BuildResult(consistent, issues);
        }

        ValidationResult BuildResult(bool consistent, List<string> issues)
        {
            StringBuilder reportBuilder = new StringBuilder();
            reportBuilder.Append("Consistency validation: ");
            reportBuilder.Append(consistent ? "PASS" : "FAIL");
            if (issues.Count > 0)
            {
                for (int i = 0; i < issues.Count; i++)
                {
                    reportBuilder.AppendLine();
                    reportBuilder.Append("- ");
                    reportBuilder.Append(issues[i]);
                }
            }

            lastReport = reportBuilder.ToString();
            if (debugLogValidation || !consistent)
            {
                Debug.Log($"[Whisperer] {lastReport}");
            }

            return new ValidationResult(consistent, issues, lastReport);
        }

        static void ValidateYearConstraints(DateTime replyDate, int knowledgeCutoffYear, string draft, List<string> issues)
        {
            int maxYear = Mathf.Min(replyDate.Year, knowledgeCutoffYear);
            MatchCollection matches = YearRegex.Matches(draft);
            for (int i = 0; i < matches.Count; i++)
            {
                if (!int.TryParse(matches[i].Value, out int year)) continue;
                if (year > maxYear)
                {
                    issues.Add($"References year {year}, which exceeds allowed year {maxYear}.");
                }
            }
        }

        static void ValidateFutureMonthReferences(DateTime replyDate, string draft, List<string> issues)
        {
            MatchCollection matches = MonthYearRegex.Matches(draft);
            for (int i = 0; i < matches.Count; i++)
            {
                string monthText = matches[i].Groups[1].Value;
                string yearText = matches[i].Groups[2].Value;
                if (!int.TryParse(yearText, out int year)) continue;
                if (!DateTime.TryParseExact(monthText, "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime monthParsed))
                    continue;

                DateTime referenced = new DateTime(year, monthParsed.Month, 1);
                DateTime limit = new DateTime(replyDate.Year, replyDate.Month, 1);
                if (referenced > limit)
                {
                    issues.Add($"References future month '{monthText} {year}'.");
                }
            }
        }

        static void ValidateFutureLedgerLeakage(DateTime replyDate, string draft, StoryEventLedger ledger, List<string> issues)
        {
            if (ledger == null) return;
            string lowered = draft.ToLowerInvariant();
            List<StoryEventEntry> snapshot = ledger.GetEntriesSnapshot();

            for (int i = 0; i < snapshot.Count; i++)
            {
                StoryEventEntry entry = snapshot[i];
                if (entry.ValidFrom <= replyDate) continue;
                if (string.IsNullOrWhiteSpace(entry.title)) continue;

                string title = entry.title.Trim().ToLowerInvariant();
                if (title.Length < 8) continue;
                if (lowered.Contains(title))
                {
                    issues.Add($"Mentions future-dated ledger title '{entry.title}' (valid from {entry.ValidFrom:yyyy-MM-dd}).");
                }
            }
        }

        static void ValidateGeneratedLetterContradictions(DateTime replyDate, string draft, StoryEventLedger ledger, List<string> issues)
        {
            if (ledger == null) return;
            string lowered = draft.ToLowerInvariant();
            List<StoryEventEntry> snapshot = ledger.GetEntriesSnapshot();

            string[] trackedTopics = { "track", "tracks", "footprint", "footprints", "sound", "sounds", "whisper", "whispers", "voices", "evidence" };
            for (int i = 0; i < trackedTopics.Length; i++)
            {
                string topic = trackedTopics[i];
                bool topicPreviouslyPresent = false;
                for (int j = 0; j < snapshot.Count; j++)
                {
                    StoryEventEntry entry = snapshot[j];
                    if (entry.ValidFrom > replyDate) continue;
                    if (!string.Equals(entry.sourceType, StoryEventMetadataValidator.SourceGeneratedLetter, StringComparison.OrdinalIgnoreCase)) continue;
                    if ((entry.description ?? string.Empty).ToLowerInvariant().Contains(topic))
                    {
                        topicPreviouslyPresent = true;
                        break;
                    }
                }

                if (!topicPreviouslyPresent) continue;

                if (lowered.Contains($"no {topic}") || lowered.Contains($"never {topic}"))
                {
                    issues.Add($"Potential contradiction: draft denies '{topic}' despite earlier generated letter context.");
                }
            }
        }

        public readonly struct ValidationResult
        {
            public readonly bool isConsistent;
            public readonly List<string> issues;
            public readonly string report;

            public ValidationResult(bool isConsistent, List<string> issues, string report)
            {
                this.isConsistent = isConsistent;
                this.issues = issues;
                this.report = report;
            }
        }
    }
}
