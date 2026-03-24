using System;
using NUnit.Framework;
using UnityEngine;
using Whisperer;

public class GameTimeManagerTests
{
    [Test]
    public void SendDate_StartsAtConfiguredOrigin()
    {
        GameObject go = new GameObject("time-manager-test");
        GameTimeManager manager = go.AddComponent<GameTimeManager>();
        manager.startYear = 1928;
        manager.startMonth = 5;
        manager.startDay = 1;

        DateTime sendDate = manager.GetSendDate();

        Assert.AreEqual(new DateTime(1928, 5, 1), sendDate);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void AdvanceTurn_MovesTimelineByOneMonth()
    {
        GameObject go = new GameObject("time-manager-test");
        GameTimeManager manager = go.AddComponent<GameTimeManager>();
        manager.startYear = 1928;
        manager.startMonth = 5;
        manager.startDay = 1;

        DateTime turn0 = manager.GetSendDate();
        manager.AdvanceTurn();
        DateTime turn1 = manager.GetSendDate();

        Assert.AreEqual(turn0.AddMonths(1), turn1);
        Assert.AreEqual(1, manager.CurrentTurn);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ReplyDate_ClampsToMonthLength()
    {
        GameObject go = new GameObject("time-manager-test");
        GameTimeManager manager = go.AddComponent<GameTimeManager>();
        manager.startYear = 1928;
        manager.startMonth = 4; // April has 30 days
        manager.startDay = 1;
        manager.replyDayOfMonth = 31;

        DateTime replyDate = manager.GetReplyDate();

        Assert.AreEqual(new DateTime(1928, 4, 30), replyDate);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ResetTurnState_ResetsCurrentTurn()
    {
        GameObject go = new GameObject("time-manager-test");
        GameTimeManager manager = go.AddComponent<GameTimeManager>();
        manager.AdvanceTurn();
        manager.AdvanceTurn();

        manager.ResetTurnState();

        Assert.AreEqual(0, manager.CurrentTurn);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void KnowledgeCutoff_UsesConfiguredYear()
    {
        GameObject go = new GameObject("time-manager-test");
        GameTimeManager manager = go.AddComponent<GameTimeManager>();
        manager.knowledgeCutoffYear = 1930;

        Assert.IsTrue(manager.IsWithinKnowledgeCutoff(new DateTime(1930, 12, 31)));
        Assert.IsFalse(manager.IsWithinKnowledgeCutoff(new DateTime(1931, 1, 1)));
        UnityEngine.Object.DestroyImmediate(go);
    }
}
