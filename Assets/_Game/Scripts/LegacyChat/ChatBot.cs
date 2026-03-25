using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine.UI;
using LLMUnity;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Whisperer
{
    public class ChatBot : MonoBehaviour
    {
        public Transform chatContainer;
        public Color playerColor = new Color32(75, 70, 80, 255);
        public Color aiColor = new Color32(70, 80, 80, 255);
        public Color fontColor = Color.white;
        public Font font;
        public int fontSize = 16;
        public int bubbleWidth = 600;
        public LLMAgent llmAgent;
        public float textPadding = 10f;
        public float bubbleSpacing = 10f;
        public Sprite sprite;
        public Button stopButton;
        [Header("P0 systems")]
        public GameTimeManager timeManager;
        public StoryEventLedger storyEventLedger;
        public LetterPromptBuilder letterPromptBuilder;
        public WeatherDataProvider weatherDataProvider;

        private InputBubble inputBubble;
        private List<Bubble> chatBubbles = new List<Bubble>();
        private bool blockInput = true;
        private BubbleUI playerUI, aiUI;
        private bool warmUpDone = false;
        private int lastBubbleOutsideFOV = -1;
        private string lastAssistantLetter = "";

        void Start()
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            playerUI = new BubbleUI
            {
                sprite = sprite,
                font = font,
                fontSize = fontSize,
                fontColor = fontColor,
                bubbleColor = playerColor,
                bottomPosition = 0,
                leftPosition = 0,
                textPadding = textPadding,
                bubbleOffset = bubbleSpacing,
                bubbleWidth = bubbleWidth,
                bubbleHeight = -1
            };
            aiUI = playerUI;
            aiUI.bubbleColor = aiColor;
            aiUI.leftPosition = 1;

            inputBubble = new InputBubble(chatContainer, playerUI, "InputBubble", "Loading...", 4);
            inputBubble.AddSubmitListener(OnInputFieldSubmit);
            inputBubble.AddValueChangedListener(OnValueChanged);
            inputBubble.setInteractable(false);
            if (stopButton != null) stopButton.gameObject.SetActive(true);
            ResolveP0Dependencies();
            ShowLoadedMessages();
            _ = llmAgent.Warmup(WarmUpCallback);
        }

        void ResolveP0Dependencies()
        {
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

        Bubble AddBubble(string message, bool isPlayerMessage)
        {
            Bubble bubble = new Bubble(chatContainer, isPlayerMessage ? playerUI : aiUI, isPlayerMessage ? "PlayerBubble" : "AIBubble", message);
            chatBubbles.Add(bubble);
            bubble.OnResize(UpdateBubblePositions);
            return bubble;
        }

        void ShowLoadedMessages()
        {
            for (int i = 1; i < llmAgent.chat.Count; i++) AddBubble(llmAgent.chat[i].content, i % 2 == 1);
        }

        void OnInputFieldSubmit(string newText)
        {
            inputBubble.ActivateInputField();
#if ENABLE_INPUT_SYSTEM
            // new input system for latest Unity version
            bool shiftHeld = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
#else
            // old input system
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
            if (blockInput || newText.Trim() == "" || shiftHeld)
            {
                StartCoroutine(BlockInteraction());
                return;
            }
            blockInput = true;
            // replace vertical_tab
            string letterBody = inputBubble.GetText().Replace("\v", "\n").Trim();

            string playerLetter = ComposePlayerLetter(letterBody);
            string modelMessage = letterBody;

            if (letterPromptBuilder != null)
            {
                llmAgent.systemPrompt = letterPromptBuilder.BuildSystemPrompt(timeManager, lastAssistantLetter, letterBody);
                modelMessage = letterPromptBuilder.BuildUserTurnPrompt(timeManager, letterBody);
            }

            AddBubble(playerLetter, true);
            Bubble aiBubble = AddBubble("...", false);
            string latestAssistantText = "";
            Task chatTask = llmAgent.Chat(
                modelMessage,
                text =>
                {
                    latestAssistantText = text;
                    aiBubble.SetText(text);
                },
                () => OnAssistantReplyCompleted(latestAssistantText)
            );
            inputBubble.SetText("");
        }

        string ComposePlayerLetter(string letterBody)
        {
            if (timeManager == null)
            {
                return letterBody;
            }

            DateTime sendDate = timeManager.GetSendDate();
            return
                $"From: Albert N. Wilmarth\n" +
                $"To: Henry W. Akeley\n" +
                $"Date: {timeManager.FormatDate(sendDate)}\n\n" +
                "My dear Mr. Akeley,\n\n" +
                letterBody + "\n\n" +
                "Yours very truly,\n" +
                "Albert N. Wilmarth";
        }

        void OnAssistantReplyCompleted(string assistantText)
        {
            lastAssistantLetter = assistantText?.Trim() ?? "";
            if (timeManager != null && storyEventLedger != null)
            {
                storyEventLedger.RecordGeneratedLetter(timeManager.GetReplyDate(), lastAssistantLetter);
                timeManager.AdvanceTurn();
            }
            AllowInput();
        }

        public void WarmUpCallback()
        {
            warmUpDone = true;
            inputBubble.SetPlaceHolderText("Write the body of your letter...");
            AllowInput();
        }

        public void AllowInput()
        {
            blockInput = false;
            inputBubble.ReActivateInputField();
        }

        public void CancelRequests()
        {
            llmAgent.CancelRequests();
            AllowInput();
        }

        IEnumerator<string> BlockInteraction()
        {
            // prevent from change until next frame
            inputBubble.setInteractable(false);
            yield return null;
            inputBubble.setInteractable(true);
            // change the caret position to the end of the text
            inputBubble.MoveTextEnd();
        }

        void OnValueChanged(string newText)
        {
            // Remove newline added by Enter
#if ENABLE_INPUT_SYSTEM
            // new input system for latest Unity version
            bool enterPressed = Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
#else
            // old input system
            bool enterPressed = Input.GetKey(KeyCode.Return);
#endif
            if (enterPressed)
            {
                if (inputBubble.GetText().Trim() == "")
                    inputBubble.SetText("");
            }
        }

        public void UpdateBubblePositions()
        {
            float y = inputBubble.GetSize().y + inputBubble.GetRectTransform().offsetMin.y + bubbleSpacing;
            float containerHeight = chatContainer.GetComponent<RectTransform>().rect.height;
            for (int i = chatBubbles.Count - 1; i >= 0; i--)
            {
                Bubble bubble = chatBubbles[i];
                RectTransform childRect = bubble.GetRectTransform();
                childRect.anchoredPosition = new Vector2(childRect.anchoredPosition.x, y);

                // last bubble outside the container
                if (y > containerHeight && lastBubbleOutsideFOV == -1)
                {
                    lastBubbleOutsideFOV = i;
                }
                y += bubble.GetSize().y + bubbleSpacing;
            }
        }

        void Update()
        {
            if (!inputBubble.inputFocused() && warmUpDone)
            {
                inputBubble.ActivateInputField();
                StartCoroutine(BlockInteraction());
            }
            if (lastBubbleOutsideFOV != -1)
            {
                // destroy bubbles outside the container
                for (int i = 0; i <= lastBubbleOutsideFOV; i++)
                {
                    chatBubbles[i].Destroy();
                }
                chatBubbles.RemoveRange(0, lastBubbleOutsideFOV + 1);
                lastBubbleOutsideFOV = -1;
            }
        }

        public void ExitGame()
        {
            Debug.Log("Exit button clicked");
            Application.Quit();
        }

        bool onValidateWarning = true;
        void OnValidate()
        {
            if (onValidateWarning && !llmAgent.remote && llmAgent.llm != null && llmAgent.llm.model == "")
            {
                Debug.LogWarning($"Please select a model in the {llmAgent.llm.gameObject.name} GameObject!");
                onValidateWarning = false;
            }
        }
    }
}
