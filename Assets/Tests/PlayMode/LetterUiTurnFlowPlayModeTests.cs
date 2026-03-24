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
    public IEnumerator SendTurn_LogsPlayerImmediately_AndAssistantOnCompletion()
    {
        SceneManager.LoadScene(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;

        LetterUiController controller = UnityEngine.Object.FindAnyObjectByType<LetterUiController>();
        Assert.NotNull(controller, "LetterUiController missing from scene.");

        FakeLetterModelClient fake = controller.gameObject.AddComponent<FakeLetterModelClient>();
        controller.modelClient = fake;

        int turnBefore = controller.timeManager.CurrentTurn;
        int historyBefore = controller.HistoryEntryCountForTests;

        Task sendTask = controller.SendTurnForTests("I write to ask if the disturbances have continued.");
        yield return null;

        Assert.AreEqual(historyBefore + 1, controller.HistoryEntryCountForTests, "Player entry should appear immediately.");

        yield return WaitForTask(sendTask, 2f);

        Assert.AreEqual(turnBefore + 1, controller.timeManager.CurrentTurn, "Turn should advance after completion.");
        Assert.AreEqual(historyBefore + 2, controller.HistoryEntryCountForTests, "Assistant entry should be appended once on completion.");

        string assistantEntry = controller.GetHistoryEntryTextForTests(controller.HistoryEntryCountForTests - 1);
        StringAssert.Contains("Akeley:", assistantEntry);
        StringAssert.Contains("hills are uneasy", assistantEntry.ToLowerInvariant());
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
