using System;
using System.Globalization;
using UnityEngine;

namespace Whisperer
{
    public class GameTimeManager : MonoBehaviour
    {
        [Header("Timeline")]
        public int startYear = 1928;
        public int startMonth = 5;
        public int startDay = 1;
        public int replyDayOfMonth = 15;
        public int knowledgeCutoffYear = 1930;

        [SerializeField] private int currentTurn = 0;

        public int CurrentTurn => currentTurn;

        public DateTime GetSendDate()
        {
            DateTime origin = new DateTime(startYear, startMonth, startDay);
            return origin.AddMonths(currentTurn);
        }

        public DateTime GetReplyDate()
        {
            DateTime sendDate = GetSendDate();
            int safeReplyDay = Mathf.Clamp(replyDayOfMonth, 1, DateTime.DaysInMonth(sendDate.Year, sendDate.Month));
            return new DateTime(sendDate.Year, sendDate.Month, safeReplyDay);
        }

        public void AdvanceTurn()
        {
            currentTurn += 1;
        }

        public bool IsWithinKnowledgeCutoff(DateTime date)
        {
            return date.Year <= knowledgeCutoffYear;
        }

        public string FormatDate(DateTime date)
        {
            return date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
        }

        public string GetTimelineSummary()
        {
            DateTime sendDate = GetSendDate();
            DateTime replyDate = GetReplyDate();
            return $"Turn {CurrentTurn + 1}: player writes {FormatDate(sendDate)}, Akeley replies in context of {FormatDate(replyDate)}.";
        }
    }
}