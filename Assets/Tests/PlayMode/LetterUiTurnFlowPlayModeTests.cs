using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Whisperer;

public class LetterUiTurnFlowPlayModeTests
{
    const string ScenePath = "Assets/_Game/Scenes/WhispererChat.unity";

    sealed class FakeLetterModelClient : LetterModelClient
    {
        public string finalReply = "My dear Wilmarth,\n\nThe hills are uneasy tonight.";
        public float delaySeconds = 0.05f;
        public override bool IsConfigured => true;

        public override async Task<string> GenerateReply(string systemPrompt, string userPrompt, Action<string> onUpdate)
        {
            onUpdate?.Invoke("My dear Wilmarth,");
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            onUpdate?.Invoke(finalReply);
            return finalReply;
        }
    }

    [UnityTest]
    public IEnumerator SendTurn_ArchivesAndDisplaysReply()
    {
        SceneManager.LoadScene(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;

        LetterUiController controller = UnityEngine.Object.FindAnyObjectByType<LetterUiController>();
        Assert.NotNull(controller, "LetterUiController missing from scene.");

        FakeLetterModelClient fake = controller.gameObject.AddComponent<FakeLetterModelClient>();
        controller.modelClient = fake;

        int turnBefore = controller.timeManager.CurrentTurn;

        Task sendTask = controller.SendTurnForTests("I write to ask if the disturbances have continued.");
        yield return null;

        yield return WaitForTask(sendTask, 2f);

        Assert.AreEqual(turnBefore + 1, controller.timeManager.CurrentTurn, "Turn should advance after completion.");
        Assert.AreEqual(1, controller.ArchiveTurnCountForTests, "Archive should contain exactly one recorded turn.");

        // Archive detail should contain full correspondence for Turn 1
        string archiveDetail = controller.ArchiveDetailTextForTests;
        StringAssert.Contains("Turn 1", archiveDetail);
        StringAssert.Contains("Wilmarth:", archiveDetail);
        StringAssert.Contains("Akeley:", archiveDetail);
        StringAssert.Contains("hills are uneasy", archiveDetail.ToLowerInvariant());
    }

    static IEnumerator WaitForTask(Task task, float timeoutSeconds)
    {
        float started = Time.realtimeSinceStartup;
        while (!task.IsCompleted)
        {
            if (Time.realtimeSinceStartup - started > timeoutSeconds)
            {
                Assert.Fail("Timed out waiting for async operation.");
            }

            yield return null;
        }

        if (task.IsFaulted)
        {
            Assert.Fail(task.Exception?.ToString() ?? "Task faulted.");
        }
    }
}
