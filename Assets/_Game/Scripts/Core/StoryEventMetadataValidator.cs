using System;
using System.Collections.Generic;
using System.Text;

namespace Whisperer
{
    public static class StoryEventMetadataValidator
    {
        public const string SourceCanon = "canon";
        public const string SourceLocal = "local";
        public const string SourceScholarly = "scholarly";
        public const string SourceInUniverse = "in-universe";
        public const string SourceGeneratedLetter = "generated-letter";
        public const string SourceSpeculative = "speculative";

        public static List<StoryEventEntry> NormalizeEntries(List<StoryEventEntry> sourceEntries, List<string> messages)
        {
            List<StoryEventEntry> normalizedEntries = new List<StoryEventEntry>();
            if (sourceEntries == null) return normalizedEntries;

            for (int i = 0; i < sourceEntries.Count; i++)
            {
                if (TryNormalize(sourceEntries[i], i, messages, out StoryEventEntry normalized))
                {
                    normalizedEntries.Add(normalized);
                }
            }

            return normalizedEntries;
        }

        public static bool TryNormalize(StoryEventEntry sourceEntry, int index, List<string> messages, out StoryEventEntry normalized)
        {
            normalized = null;
            if (sourceEntry == null)
            {
                messages?.Add($"Story entry #{index} rejected: entry was null.");
                return false;
            }

            StoryEventEntry candidate = sourceEntry.Clone();
            string context = $"story entry #{index}";

            if (string.IsNullOrWhiteSpace(candidate.id))
            {
                candidate.id = BuildGeneratedId(candidate.title, index);
                messages?.Add($"{context} missing id; generated '{candidate.id}'.");
            }

            context = $"story entry '{candidate.id}'";

            if (string.IsNullOrWhiteSpace(candidate.title))
            {
                messages?.Add($"{context} rejected: missing title.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(candidate.description))
            {
                messages?.Add($"{context} rejected: missing description.");
                return false;
            }

            if (!TryNormalizeSourceType(candidate.sourceType, out string normalizedSourceType, out string sourceWarning))
            {
                messages?.Add($"{context} rejected: sourceType '{candidate.sourceType}' is not supported.");
                return false;
            }

            candidate.sourceType = normalizedSourceType;
            if (!string.IsNullOrWhiteSpace(sourceWarning))
            {
                messages?.Add($"{context}: {sourceWarning}");
            }

            if (!TryBuildDate(candidate.validFromYear, candidate.validFromMonth, candidate.validFromDay, out DateTime validFrom))
            {
                messages?.Add($"{context} rejected: validFrom date is invalid.");
                return false;
            }

            if (candidate.hasEndDate)
            {
                if (!TryBuildDate(candidate.validToYear, candidate.validToMonth, candidate.validToDay, out DateTime validTo))
                {
                    messages?.Add($"{context} rejected: validTo date is invalid.");
                    return false;
                }

                if (validTo < validFrom)
                {
                    messages?.Add($"{context} rejected: validTo is earlier than validFrom.");
                    return false;
                }
            }
            else
            {
                candidate.validToYear = 0;
                candidate.validToMonth = 0;
                candidate.validToDay = 0;
            }

            if (candidate.reliability <= 0)
            {
                candidate.reliability = GetDefaultReliability(candidate.sourceType);
                messages?.Add($"{context} missing reliability; defaulted to {candidate.reliability}.");
            }
            else if (candidate.reliability > 100)
            {
                candidate.reliability = 100;
                messages?.Add($"{context} reliability exceeded 100; clamped to 100.");
            }

            if (candidate.tags == null)
            {
                candidate.tags = new List<string>();
                messages?.Add($"{context} missing tags; defaulted to an empty tag list.");
            }
            else
            {
                candidate.tags = SanitizeTags(candidate.tags);
            }

            normalized = candidate;
            return true;
        }

        public static string BuildSummary(string sourceName, int sourceCount, int acceptedCount, List<string> messages)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Metadata validation for ");
            builder.Append(sourceName);
            builder.Append(": accepted ");
            builder.Append(acceptedCount);
            builder.Append(" of ");
            builder.Append(sourceCount);
            builder.Append(" entries.");

            if (messages != null && messages.Count > 0)
            {
                for (int i = 0; i < messages.Count; i++)
                {
                    builder.AppendLine();
                    builder.Append("- ");
                    builder.Append(messages[i]);
                }
            }

            return builder.ToString();
        }

        private static List<string> SanitizeTags(List<string> tags)
        {
            List<string> sanitized = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i]?.Trim();
                if (string.IsNullOrWhiteSpace(tag)) continue;
                if (!seen.Add(tag)) continue;
                sanitized.Add(tag);
            }

            return sanitized;
        }

        private static bool TryNormalizeSourceType(string rawSourceType, out string normalizedSourceType, out string warning)
        {
            warning = null;
            normalizedSourceType = null;

            if (string.IsNullOrWhiteSpace(rawSourceType))
            {
                return false;
            }

            string trimmed = rawSourceType.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case SourceCanon:
                case SourceLocal:
                case SourceScholarly:
                case SourceInUniverse:
                case SourceGeneratedLetter:
                case SourceSpeculative:
                    normalizedSourceType = trimmed;
                    return true;
                case "local-context":
                    normalizedSourceType = SourceLocal;
                    warning = "sourceType 'local-context' is deprecated; normalized to 'local'.";
                    return true;
                default:
                    return false;
            }
        }

        private static int GetDefaultReliability(string sourceType)
        {
            switch (sourceType)
            {
                case SourceCanon:
                    return 100;
                case SourceLocal:
                    return 85;
                case SourceScholarly:
                    return 75;
                case SourceInUniverse:
                    return 65;
                case SourceGeneratedLetter:
                    return 80;
                case SourceSpeculative:
                    return 40;
                default:
                    return 50;
            }
        }

        private static bool TryBuildDate(int year, int month, int day, out DateTime date)
        {
            date = default;
            try
            {
                date = new DateTime(year, month, day);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        private static string BuildGeneratedId(string title, int index)
        {
            if (string.IsNullOrWhiteSpace(title)) return $"story-entry-{index}";

            StringBuilder builder = new StringBuilder();
            bool lastWasSeparator = false;

            for (int i = 0; i < title.Length; i++)
            {
                char character = char.ToLowerInvariant(title[i]);
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                    lastWasSeparator = false;
                    continue;
                }

                if (lastWasSeparator || builder.Length == 0) continue;
                builder.Append('-');
                lastWasSeparator = true;
            }

            string slug = builder.ToString().Trim('-');
            if (string.IsNullOrWhiteSpace(slug)) slug = $"story-entry-{index}";
            return slug;
        }
    }
}