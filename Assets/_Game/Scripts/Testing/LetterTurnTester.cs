using System;
using System.Text;
using LLMUnity;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Whisperer
{
    public class LetterTurnTester : MonoBehaviour
    {
        [Header("References")]
        public LLMAgent llmAgent;
        public GameTimeManager timeManager;
        public StoryEventLedger storyEventLedger;
        public LetterPromptBuilder letterPromptBuilder;
        public WeatherDataProvider weatherDataProvider;

        [Header("Test input")]
        [TextArea(4, 10)]
        public string playerLetterBody = "My dear Mr. Akeley,\n\nI write to ask whether the disturbances near your farm have continued through the month.";
        public KeyCode sendTurnKey = KeyCode.Return;
        public bool requireShift = true;

        [Header("Runtime")]
        [TextArea(4, 12)]
        public string lastAssistantLetter;

        bool requestInFlight;

        void Awake()
        {
            if (llmAgent == null) llmAgent = FindAnyObjectByType<LLMAgent>();
            if (timeManager == null) timeManager = FindAnyObjectByType<GameTimeManager>();
            if (storyEventLedger == null) storyEventLedger = FindAnyObjectByType<StoryEventLedger>();
            if (letterPromptBuilder == null) letterPromptBuilder = FindAnyObjectByType<LetterPromptBuilder>();
            if (weatherDataProvider == null) weatherDataProvider = FindAnyObjectByType<WeatherDataProvider>();

            if (timeManager == null) timeManager = gameObject.AddComponent<GameTimeManager>();
            if (storyEventLedger == null) storyEventLedger = gameObject.AddComponent<StoryEventLedger>();
            if (letterPromptBuilder == null) letterPromptBuilder = gameObject.AddComponent<LetterPromptBuilder>();
            if (weatherDataProvider == null) weatherDataProvider = gameObject.AddComponent<WeatherDataProvider>();

            if (storyEventLedger.seedJson == null)
            {
                storyEventLedger.seedJson = Resources.Load<TextAsset>("Whisperer/story-events");
            }
            storyEventLedger.EnsureLoaded();
            letterPromptBuilder.storyEventLedger = storyEventLedger;
            letterPromptBuilder.weatherDataProvider = weatherDataProvider;
        }

        void Update()
        {
            if (requestInFlight || llmAgent == null) return;

            bool enterPressed = IsSendTurnPressed();
            if (!enterPressed) return;

            bool shiftHeld = IsShiftHeld();
            if (requireShift && !shiftHeld) return;

            if (llmAgent.llm == null || string.IsNullOrWhiteSpace(llmAgent.llm.model))
            {
                Debug.LogError("[Whisperer] LLMAgent is not fully configured. Assign an LLM and model file in the LLMStack object before running a turn.");
                return;
            }

            _ = RunTurn();
        }

            bool IsSendTurnPressed()
            {
        #if ENABLE_INPUT_SYSTEM
                if (Keyboard.current == null) return false;

                if (sendTurnKey == KeyCode.Return || sendTurnKey == KeyCode.KeypadEnter)
                {
                return Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
                }

                return false;
        #else
                return Input.GetKeyDown(sendTurnKey);
        #endif
            }

            bool IsShiftHeld()
            {
        #if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        #else
                return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        #endif
            }

        async System.Threading.Tasks.Task RunTurn()
        {
            requestInFlight = true;
            DateTime sendDate = timeManager.GetSendDate();
            DateTime replyDate = timeManager.GetReplyDate();

            llmAgent.systemPrompt = letterPromptBuilder.BuildSystemPrompt(timeManager, lastAssistantLetter, playerLetterBody);
            string userPrompt = letterPromptBuilder.BuildUserTurnPrompt(timeManager, playerLetterBody);

            Debug.Log($"[Whisperer] Running turn {timeManager.CurrentTurn + 1} | Send: {timeManager.FormatDate(sendDate)} | Reply context: {timeManager.FormatDate(replyDate)}");
            Debug.Log($"[Whisperer] Timeline summary: {timeManager.GetTimelineSummary()}");

            StringBuilder stream = new StringBuilder();
            try
            {
                await llmAgent.Chat(
                    userPrompt,
                    token =>
                    {
                        stream.Append(token);
                    }
                );
                lastAssistantLetter = stream.ToString().Trim();
                storyEventLedger.RecordGeneratedLetter(replyDate, lastAssistantLetter);
                timeManager.AdvanceTurn();

                Debug.Log($"[Whisperer] Assistant letter complete. Length={lastAssistantLetter.Length}");
                Debug.Log(lastAssistantLetter);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Whisperer] Turn failed: {ex.Message}");
            }
            finally
            {
                requestInFlight = false;
            }
        }
    }
}