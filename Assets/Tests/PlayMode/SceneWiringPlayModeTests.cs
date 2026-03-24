using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Whisperer;

public class SceneWiringPlayModeTests
{
    const string ScenePath = "Assets/_Game/Scenes/WhispererChat.unity";

    [UnityTest]
    public IEnumerator WhispererChatScene_HasRequiredWiring()
    {
        Scene scene = SceneManager.LoadScene(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;

        Assert.IsTrue(scene.isLoaded, "WhispererChat scene failed to load.");

        GameObject llmStack = GameObject.Find("LLMStack");
        GameObject systems = GameObject.Find("WhispererSystems");
        GameObject letterUi = GameObject.Find("LetterUI");

        Assert.NotNull(llmStack, "Expected LLMStack root object.");
        Assert.NotNull(systems, "Expected WhispererSystems root object.");
        Assert.NotNull(letterUi, "Expected LetterUI root object.");

        LetterUiController controller = Object.FindAnyObjectByType<LetterUiController>();
        Assert.NotNull(controller, "LetterUiController is missing from scene.");

        Assert.NotNull(controller.GetComponent<UnityEngine.UIElements.UIDocument>(), "UIDocument is missing from LetterUI.");
        Assert.NotNull(controller.timeManager, "GameTimeManager reference not resolved.");
        Assert.NotNull(controller.storyEventLedger, "StoryEventLedger reference not resolved.");
        Assert.NotNull(controller.letterPromptBuilder, "LetterPromptBuilder reference not resolved.");

        Assert.NotNull(controller.storyEventLedger.seedJson, "Seed story-events JSON should be assigned/resolved.");
    }
}
