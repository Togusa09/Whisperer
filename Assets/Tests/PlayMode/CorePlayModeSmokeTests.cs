using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Whisperer;

public class CorePlayModeSmokeTests
{
    [UnityTest]
    public IEnumerator GameTimeManager_AdvanceTurn_UpdatesCurrentTurnInPlayMode()
    {
        GameObject go = new GameObject("playmode-time-manager");
        GameTimeManager manager = go.AddComponent<GameTimeManager>();

        yield return null;

        Assert.AreEqual(0, manager.CurrentTurn);
        manager.AdvanceTurn();
        Assert.AreEqual(1, manager.CurrentTurn);

        Object.Destroy(go);
    }
}
