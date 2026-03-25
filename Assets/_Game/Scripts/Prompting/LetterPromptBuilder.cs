using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Whisperer
{
    public class LetterPromptBuilder : MonoBehaviour
    {
        public StoryEventLedger storyEventLedger;
        [Range(1, 20)] public int maxLedgerEntries = 6;

        [Header("Retrieval")]
        public bool useRetrievalPipeline = true;
        public bool includeCanon = true;
        public bool includeLocal = true;
        public bool includeScholarly = true;
        public bool includeInUniverse = true;
        public bool includeSpeculative = true;
        public bool includeGeneratedLetters = true;

        [Header("Debug")]
        public bool debugLogPrompts;
        [TextArea(8, 20)]
        [SerializeField] string lastSystemPrompt = "";
        [TextArea(6, 16)]
        [SerializeField] string lastUserPrompt = "";
        [TextArea(6, 16)]
        [SerializeField] string lastRetrievalTrace = "";
        [TextArea(5, 14)]
        [SerializeField] string lastSourceFraming = "";

        public string LastSystemPrompt => lastSystemPrompt;
        public string LastUserPrompt => lastUserPrompt;
        public string LastRetrievalTrace => lastRetrievalTrace;
        public string LastSourceFraming => lastSourceFraming;

        public string BuildSystemPrompt(GameTimeManager timeManager, string previousAssistantLetter)
        {
            if (timeManager == null)
            {
                lastSystemPrompt = "You are Henry W. Akeley writing period letters to Albert N. Wilmarth.";
                return lastSystemPrompt;
            }

            DateTime sendDate = timeManager.GetSendDate();
            DateTime replyDate = timeManager.GetReplyDate();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("You are Henry W. Akeley in a historical letter correspondence with Albert N. Wilmarth.");
            builder.AppendLine("Write in period-appropriate epistolary style.");
            builder.AppendLine();
            builder.AppendLine("Hard rules:");
            builder.AppendLine("- The player writes at the start of the month.");
            builder.AppendLine("- You reply in the context of mid-month of that same month.");
            builder.AppendLine("- Do not reference future narrative events beyond the current timeline.");
            builder.AppendLine($"- Do not use real-world knowledge after {timeManager.knowledgeCutoffYear}.");
            builder.AppendLine("- You may speculate only using period-appropriate ideas available by 1930.");
            builder.AppendLine();
            builder.AppendLine("Current timeline:");
            builder.AppendLine($"- Player send date: {timeManager.FormatDate(sendDate)}");
            builder.AppendLine($"- Your reply context date: {timeManager.FormatDate(replyDate)}");

            if (!string.IsNullOrWhiteSpace(previousAssistantLetter))
            {
                string previous = previousAssistantLetter.Trim();
                if (previous.Length > 700) previous = previous.Substring(0, 700);
                builder.AppendLine();
                builder.AppendLine("Your previous letter summary:");
                builder.AppendLine(previous);
            }

            if (storyEventLedger != null)
            {
                string contextBlock = BuildRetrievalContext(replyDate);
                if (!string.IsNullOrWhiteSpace(contextBlock))
                {
                    builder.AppendLine();
                    builder.AppendLine(contextBlock);
                }

                if (!string.IsNullOrWhiteSpace(lastSourceFraming))
                {
                    builder.AppendLine();
                    builder.AppendLine(lastSourceFraming);
                }
            }

            builder.AppendLine();
            builder.AppendLine("Response format:");
            builder.AppendLine("- Return only the letter text from Henry W. Akeley.");
            builder.AppendLine("- Include concrete developments since your previous letter.");
            lastSystemPrompt = builder.ToString().Trim();

            if (debugLogPrompts)
            {
                Debug.Log($"[Whisperer] System prompt built:\n{lastSystemPrompt}");
                if (!string.IsNullOrWhiteSpace(lastRetrievalTrace))
                {
                    Debug.Log($"[Whisperer] {lastRetrievalTrace}");
                }
                if (!string.IsNullOrWhiteSpace(lastSourceFraming))
                {
                    Debug.Log($"[Whisperer] {lastSourceFraming}");
                }
            }

            return lastSystemPrompt;
        }

        public string BuildUserTurnPrompt(GameTimeManager timeManager, string playerBody)
        {
            if (timeManager == null)
            {
                lastUserPrompt = playerBody;
                return lastUserPrompt;
            }

            DateTime sendDate = timeManager.GetSendDate();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Letter from Albert N. Wilmarth dated {timeManager.FormatDate(sendDate)}:");
            builder.AppendLine(playerBody.Trim());
            builder.AppendLine();
            builder.AppendLine("Reply as Henry W. Akeley in letter form.");
            lastUserPrompt = builder.ToString().Trim();

            if (debugLogPrompts)
            {
                Debug.Log($"[Whisperer] User prompt built:\n{lastUserPrompt}");
            }

            return lastUserPrompt;
        }

        string BuildRetrievalContext(DateTime replyDate)
        {
            if (!useRetrievalPipeline)
            {
                lastRetrievalTrace = "";
                lastSourceFraming = "";
                return storyEventLedger.BuildContextBlock(replyDate, maxLedgerEntries);
            }

            HashSet<string> allowedSourceTypes = BuildAllowedSourceTypes();
            StoryRetrievalPipeline.RetrievalResult retrievalResult = StoryRetrievalPipeline.Retrieve(
                storyEventLedger,
                replyDate,
                maxLedgerEntries,
                allowedSourceTypes,
                debugLogPrompts);

            lastRetrievalTrace = retrievalResult.trace;
            lastSourceFraming = StoryRetrievalPipeline.BuildSourceFramingBlock(retrievalResult.entries);
            return StoryRetrievalPipeline.BuildContextBlock(retrievalResult.entries);
        }

        HashSet<string> BuildAllowedSourceTypes()
        {
            HashSet<string> sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (includeCanon) sources.Add(StoryEventMetadataValidator.SourceCanon);
            if (includeLocal) sources.Add(StoryEventMetadataValidator.SourceLocal);
            if (includeScholarly) sources.Add(StoryEventMetadataValidator.SourceScholarly);
            if (includeInUniverse) sources.Add(StoryEventMetadataValidator.SourceInUniverse);
            if (includeSpeculative) sources.Add(StoryEventMetadataValidator.SourceSpeculative);
            if (includeGeneratedLetters) sources.Add(StoryEventMetadataValidator.SourceGeneratedLetter);
            return sources;
        }
    }
}