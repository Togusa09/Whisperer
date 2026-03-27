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

    sealed class FakeStreamingLetterModelClient : LetterModelClient
    {
        public string[] streamedUpdates =
        {
            "My dear Wilmarth,",
            "My dear Wilmarth,\n\nThe hills are uneasy tonight."
        };
        public float initialDelaySeconds;
        public float betweenUpdatesDelaySeconds = 0.05f;
        public override bool IsConfigured => true;

        public override async Task<string> GenerateReply(string systemPrompt, string userPrompt, Action<string> onUpdate)
        {
            if (initialDelaySeconds > 0f)
            {
                await Task.Delay(TimeSpan.FromSeconds(initialDelaySeconds));
            }

            for (int i = 0; i < streamedUpdates.Length; i++)
            {
                onUpdate?.Invoke(streamedUpdates[i]);
                if (i < streamedUpdates.Length - 1 && betweenUpdatesDelaySeconds > 0f)
                {
                    await Task.Delay(TimeSpan.FromSeconds(betweenUpdatesDelaySeconds));
                }
            }

            return streamedUpdates.Length > 0 ? streamedUpdates[streamedUpdates.Length - 1] : string.Empty;
        }
    }

    [UnityTest]
    public IEnumerator SendTurn_ArchivesAndDisplaysReply()
    {
        SceneManager.LoadScene(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;

        LetterUiController controller = UnityEngine.Object.FindAnyObjectByType<LetterUiController>();
        Assert.NotNull(controller, "LetterUiController missing from scene.");

        FakeStreamingLetterModelClient fake = controller.gameObject.AddComponent<FakeStreamingLetterModelClient>();
        controller.minReplyRevealSeconds = 0.01f;
        controller.maxReplyRevealSeconds = 0.01f;
        controller.modelClient = fake;

        int turnBefore = controller.timeManager.CurrentTurn;
        int archiveCountBefore = controller.ArchiveTurnCountForTests;

        Task sendTask = controller.SendTurnForTests("I write to ask if the disturbances have continued.");
        yield return null;

        yield return WaitForTask(sendTask, 2f);

        Assert.AreEqual(turnBefore + 1, controller.timeManager.CurrentTurn, "Turn should advance after completion.");
        Assert.AreEqual(archiveCountBefore + 1, controller.ArchiveTurnCountForTests, "Archive should contain one additional recorded turn.");

        // Archive detail should contain full correspondence for Turn 1
        string archiveDetail = controller.ArchiveDetailTextForTests;
        StringAssert.Contains("Turn 1", archiveDetail);
        StringAssert.Contains("Wilmarth:", archiveDetail);
        StringAssert.Contains("Akeley:", archiveDetail);
        StringAssert.Contains("hills are uneasy", archiveDetail.ToLowerInvariant());
    }

    [UnityTest]
    public IEnumerator ClosingReply_ReEnablesSendButton()
    {
        SceneManager.LoadScene(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;

        LetterUiController controller = UnityEngine.Object.FindAnyObjectByType<LetterUiController>();
        Assert.NotNull(controller, "LetterUiController missing from scene.");

        FakeStreamingLetterModelClient fake = controller.gameObject.AddComponent<FakeStreamingLetterModelClient>();
        controller.minReplyRevealSeconds = 0.01f;
        controller.maxReplyRevealSeconds = 0.01f;
        controller.modelClient = fake;

        Task sendTask = controller.SendTurnForTests("I write to ask if the disturbances have continued.");
        yield return null;

        yield return WaitForTask(sendTask, 2f);

        controller.OpenLatestReplyForTests();
        yield return null;

        Assert.IsTrue(controller.IsDeskLetterVisibleForTests, "Reply letter should be visible after opening it.");
        Assert.IsFalse(controller.SendButtonEnabledForTests, "Send button should remain disabled while reply is ready and open.");
        Assert.IsTrue(controller.ReplyCloseEnabledForTests, "Reply should be closable once generation is complete.");

        controller.ReturnDeskLetterToFileForTests();
        yield return null;

        Assert.IsFalse(controller.IsDeskLetterVisibleForTests, "Reply letter should be hidden after returning it to file.");
        Assert.IsTrue(controller.SendButtonEnabledForTests, "Send button should be re-enabled once the composer returns to compose state.");
        Assert.AreEqual("Ready", controller.StatusTextForTests);
    }

    [UnityTest]
    public IEnumerator ReplyReveal_AllowsOpeningBeforeGenerationCompletes()
    {
        SceneManager.LoadScene(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;

        LetterUiController controller = UnityEngine.Object.FindAnyObjectByType<LetterUiController>();
        Assert.NotNull(controller, "LetterUiController missing from scene.");

        FakeStreamingLetterModelClient fake = controller.gameObject.AddComponent<FakeStreamingLetterModelClient>();
        fake.initialDelaySeconds = 0f;
        fake.betweenUpdatesDelaySeconds = 0.35f;
        controller.minReplyRevealSeconds = 0.05f;
        controller.maxReplyRevealSeconds = 0.05f;
        controller.modelClient = fake;

        Task sendTask = controller.SendTurnForTests("I write to ask if the disturbances have continued.");
        yield return null;

        yield return WaitUntil(() => controller.ReplyReadyToOpenForTests, 1f);
        Assert.IsFalse(sendTask.IsCompleted, "Send should still be in flight while the reply continues streaming.");

        controller.OpenLatestReplyForTests();
        yield return null;

        Assert.IsTrue(controller.IsDeskLetterVisibleForTests, "Reply letter should open once the reveal delay elapses.");
        Assert.IsFalse(controller.ReplyGenerationCompleteForTests, "Reply generation should still be in progress.");
        Assert.IsFalse(controller.ReplyCloseEnabledForTests, "Return to file should remain locked until generation completes.");
        StringAssert.Contains("My dear Wilmarth", controller.DeskLetterBodyForTests);

        yield return WaitForTask(sendTask, 2f);

        Assert.IsTrue(controller.ReplyGenerationCompleteForTests, "Reply generation should complete after the final streamed chunk.");
        Assert.IsTrue(controller.ReplyCloseEnabledForTests, "Return to file should unlock when the reply is complete.");
        StringAssert.Contains("hills are uneasy", controller.DeskLetterBodyForTests.ToLowerInvariant());
    }

    [UnityTest]
    public IEnumerator OpeningBeforeFirstChunk_ShowsPlaceholderUntilTextArrives()
    {
        SceneManager.LoadScene(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;

        LetterUiController controller = UnityEngine.Object.FindAnyObjectByType<LetterUiController>();
        Assert.NotNull(controller, "LetterUiController missing from scene.");

        FakeStreamingLetterModelClient fake = controller.gameObject.AddComponent<FakeStreamingLetterModelClient>();
        fake.initialDelaySeconds = 0.2f;
        fake.betweenUpdatesDelaySeconds = 0.05f;
        controller.minReplyRevealSeconds = 0.05f;
        controller.maxReplyRevealSeconds = 0.05f;
        controller.modelClient = fake;

        Task sendTask = controller.SendTurnForTests("I write to ask if the disturbances have continued.");
        yield return null;

        yield return WaitUntil(() => controller.ReplyReadyToOpenForTests, 1f);

        controller.OpenLatestReplyForTests();
        yield return null;

        StringAssert.Contains("still moving across the page", controller.DeskLetterBodyForTests);
        Assert.IsFalse(controller.ReplyCloseEnabledForTests, "Return to file should remain locked while no final reply exists.");

        yield return WaitUntil(() => controller.DeskLetterBodyForTests.Contains("My dear Wilmarth"), 1f);
        StringAssert.Contains("My dear Wilmarth", controller.DeskLetterBodyForTests);

        yield return WaitForTask(sendTask, 2f);
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

    static IEnumerator WaitUntil(Func<bool> predicate, float timeoutSeconds)
    {
        float started = Time.realtimeSinceStartup;
        while (!predicate())
        {
            if (Time.realtimeSinceStartup - started > timeoutSeconds)
            {
                Assert.Fail("Timed out waiting for condition.");
            }

            yield return null;
        }
    }
}
