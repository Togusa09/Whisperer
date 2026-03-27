using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
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

        yield return WaitForControllerInScene(ScenePath, 5f);
        LetterUiController controller = UnityEngine.Object.FindAnyObjectByType<LetterUiController>();
        Assert.NotNull(controller, "LetterUiController missing from scene.");
        DisableWarmupForTests(controller);

        FakeLetterModelClient fake = controller.gameObject.AddComponent<FakeLetterModelClient>();
        controller.modelClient = fake;

        yield return WaitForControllerReady(controller, 5f);

        int turnBefore = controller.timeManager.CurrentTurn;
        int archiveBefore = controller.ArchiveTurnCountForTests;

        Task sendTask = controller.SendTurnForTests("I write to ask if the disturbances have continued.");
        yield return null;

        yield return WaitForTask(sendTask, 2f);

        Assert.AreEqual(turnBefore + 1, controller.timeManager.CurrentTurn, "Turn should advance after completion.");
        Assert.AreEqual(archiveBefore + 1, controller.ArchiveTurnCountForTests, "Archive should append one recorded turn.");

        // Archive detail should contain full correspondence for Turn 1
        string archiveDetail = controller.ArchiveDetailTextForTests;
        StringAssert.Contains("May 1, 1928", archiveDetail);
        StringAssert.Contains("Wilmarth:", archiveDetail);
        StringAssert.Contains("Akeley:", archiveDetail);
        StringAssert.Contains("hills are uneasy", archiveDetail.ToLowerInvariant());
    }

    [UnityTest]
    public IEnumerator ClosingReply_ReEnablesSendButton()
    {
        SceneManager.LoadScene(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));

        yield return WaitForControllerInScene(ScenePath, 5f);
        LetterUiController controller = UnityEngine.Object.FindAnyObjectByType<LetterUiController>();
        Assert.NotNull(controller, "LetterUiController missing from scene.");
        DisableWarmupForTests(controller);

        FakeLetterModelClient fake = controller.gameObject.AddComponent<FakeLetterModelClient>();
        controller.modelClient = fake;

        yield return WaitForControllerReady(controller, 5f);

        Task sendTask = controller.SendTurnForTests("I write to ask if the disturbances have continued.");
        yield return null;

        yield return WaitForTask(sendTask, 2f);

        Assert.IsFalse(controller.IsDeskLetterVisibleForTests, "Reply letter should stay closed until explicitly opened.");
        Assert.IsFalse(controller.SendButtonEnabledForTests, "Send button should remain disabled while reply is ready and open.");

        InvokePrivate(controller, "OnComposerNotificationButtonClicked");
        yield return null;

        Assert.IsTrue(controller.IsDeskLetterVisibleForTests, "Reply letter should be visible after opening from the composer notification.");

        controller.ReturnDeskLetterToFileForTests();
        yield return null;

        Assert.IsFalse(controller.IsDeskLetterVisibleForTests, "Reply letter should be hidden after returning it to file.");
        Assert.IsTrue(controller.SendButtonEnabledForTests, "Send button should be re-enabled once the composer returns to compose state.");
        Assert.AreEqual("Ready", controller.StatusTextForTests);
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

    static IEnumerator WaitForControllerReady(LetterUiController controller, float timeoutSeconds)
    {
        Assert.NotNull(controller, "Controller is null.");

        float started = Time.realtimeSinceStartup;
        while (controller != null && (controller.DiagnosticsIsWarmupInFlight || controller.DiagnosticsIsRequestInFlight))
        {
            if (Time.realtimeSinceStartup - started > timeoutSeconds)
            {
                Assert.Fail($"Controller did not become ready within timeout. status='{controller.StatusTextForTests}'");
            }

            yield return null;
        }

        if (controller == null)
        {
            Assert.Fail("LetterUiController was destroyed while waiting for readiness.");
        }
    }

    static IEnumerator WaitForControllerInScene(string expectedScenePath, float timeoutSeconds)
    {
        float started = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - started <= timeoutSeconds)
        {
            Scene active = SceneManager.GetActiveScene();
            bool sceneReady = active.IsValid() && active.path == expectedScenePath;
            if (sceneReady)
            {
                LetterUiController controller = UnityEngine.Object.FindAnyObjectByType<LetterUiController>();
                if (controller != null)
                {
                    yield break;
                }
            }

            yield return null;
        }

        Assert.Fail($"LetterUiController missing from scene '{expectedScenePath}'.");
    }

    static void DisableWarmupForTests(LetterUiController controller)
    {
        controller.warmupOnStart = false;

        SetPrivateField(controller, "warmupInFlight", false);
        SetPrivateField(controller, "startupWarmupRequested", true);

        // Mirror production state refresh after changing warmup flags.
        InvokePrivate(controller, "UpdateControlStates");

        UIDocument document = controller.GetComponent<UIDocument>();
        if (document != null && document.rootVisualElement != null && document.rootVisualElement.childCount == 0)
        {
            InvokePrivate(controller, "EnsureUiBuilt");
        }
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method, $"Method '{methodName}' not found on {target.GetType().Name}.");
        method.Invoke(target, null);
    }
}
