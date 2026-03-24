using System;
using System.Text;
using UnityEngine;

namespace Whisperer
{
    public class LetterPromptBuilder : MonoBehaviour
    {
        public StoryEventLedger storyEventLedger;
        [Range(1, 20)] public int maxLedgerEntries = 6;

        public string BuildSystemPrompt(GameTimeManager timeManager, string previousAssistantLetter)
        {
            if (timeManager == null)
            {
                return "You are Henry W. Akeley writing period letters to Albert N. Wilmarth.";
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
                string contextBlock = storyEventLedger.BuildContextBlock(replyDate, maxLedgerEntries);
                if (!string.IsNullOrWhiteSpace(contextBlock))
                {
                    builder.AppendLine();
                    builder.AppendLine(contextBlock);
                }
            }

            builder.AppendLine();
            builder.AppendLine("Response format:");
            builder.AppendLine("- Return only the letter text from Henry W. Akeley.");
            builder.AppendLine("- Include concrete developments since your previous letter.");
            return builder.ToString().Trim();
        }

        public string BuildUserTurnPrompt(GameTimeManager timeManager, string playerBody)
        {
            if (timeManager == null) return playerBody;

            DateTime sendDate = timeManager.GetSendDate();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Letter from Albert N. Wilmarth dated {timeManager.FormatDate(sendDate)}:");
            builder.AppendLine(playerBody.Trim());
            builder.AppendLine();
            builder.AppendLine("Reply as Henry W. Akeley in letter form.");
            return builder.ToString().Trim();
        }
    }
}