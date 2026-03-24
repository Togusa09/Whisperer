using System;
using System.Text;
using System.Threading.Tasks;
using LLMUnity;
using UnityEngine;
using UnityEngine.UIElements;

namespace Whisperer
{
    [RequireComponent(typeof(UIDocument))]
    public class LetterUiController : MonoBehaviour
    {
        const string LayoutResourcePath = "Whisperer/UI/LetterUI";
        const string StyleResourcePath = "Whisperer/UI/LetterUITheme";

        [Header("References")]
        public LLMAgent llmAgent;
        public GameTimeManager timeManager;
        public StoryEventLedger storyEventLedger;
        public LetterPromptBuilder letterPromptBuilder;
        public PanelSettings panelSettingsAsset;

        [Header("Template")]
        public string fromName = "Albert N. Wilmarth";
        public string toName = "Henry W. Akeley";
        public string fromAddress = "Miskatonic University, Arkham, Massachusetts";
        public string toAddress = "R.F.D. #2, Townshend, Windham County, Vermont";

        UIDocument uiDocument;
        Label turnLabel;
        Label toLabel;
        Label fromLabel;
        Label dateLabel;
        Label toAddressLabel;
        Label fromAddressLabel;
        TextField bodyField;
        Button sendButton;
        Label statusLabel;
        ScrollView historyView;

        bool requestInFlight;
        bool uiBuilt;
        string lastAssistantLetter = "";
        VisualTreeAsset layoutAsset;
        StyleSheet styleSheet;

        void Awake()
        {
            ResolveDependencies();
        }

        void Start()
        {
            EnsureUiBuilt();
        }

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            EnsureUiBuilt();
        }

        void ResolveDependencies()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument.panelSettings == null && panelSettingsAsset != null)
            {
                uiDocument.panelSettings = panelSettingsAsset;
            }

            layoutAsset = Resources.Load<VisualTreeAsset>(LayoutResourcePath);
            styleSheet = Resources.Load<StyleSheet>(StyleResourcePath);

            if (llmAgent == null) llmAgent = FindAnyObjectByType<LLMAgent>();
            if (timeManager == null) timeManager = FindAnyObjectByType<GameTimeManager>();
            if (storyEventLedger == null) storyEventLedger = FindAnyObjectByType<StoryEventLedger>();
            if (letterPromptBuilder == null) letterPromptBuilder = FindAnyObjectByType<LetterPromptBuilder>();

            if (timeManager == null) timeManager = gameObject.AddComponent<GameTimeManager>();
            if (storyEventLedger == null) storyEventLedger = gameObject.AddComponent<StoryEventLedger>();
            if (letterPromptBuilder == null) letterPromptBuilder = gameObject.AddComponent<LetterPromptBuilder>();

            if (storyEventLedger.seedJson == null)
            {
                storyEventLedger.seedJson = Resources.Load<TextAsset>("Whisperer/story-events");
            }
            storyEventLedger.EnsureLoaded();
            letterPromptBuilder.storyEventLedger = storyEventLedger;
        }

        void BuildUi()
        {
            VisualElement root = uiDocument.rootVisualElement;
            root.Clear();
            if (uiDocument.panelSettings == null)
            {
                statusLabel = new Label("Missing PanelSettings asset on UIDocument/LetterUiController.");
                root.Add(statusLabel);
                Debug.LogError("[Whisperer] LetterUiController requires an asset-backed PanelSettings reference.");
                return;
            }

            if (layoutAsset == null)
            {
                statusLabel = new Label($"Missing layout resource at Resources/{LayoutResourcePath}.uxml");
                root.Add(statusLabel);
                return;
            }

            layoutAsset.CloneTree(root);
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            turnLabel = root.Q<Label>("TurnLabel");
            toLabel = root.Q<Label>("ToLabel");
            fromLabel = root.Q<Label>("FromLabel");
            dateLabel = root.Q<Label>("DateLabel");
            toAddressLabel = root.Q<Label>("ToAddressLabel");
            fromAddressLabel = root.Q<Label>("FromAddressLabel");
            bodyField = root.Q<TextField>("BodyField");
            sendButton = root.Q<Button>("SendButton");
            statusLabel = root.Q<Label>("StatusLabel");
            historyView = root.Q<ScrollView>("HistoryView");

            if (bodyField != null)
            {
                bodyField.multiline = true;
                bodyField.value = "My dear Mr. Akeley,\n\n";
            }

            if (sendButton != null)
            {
                sendButton.clicked += () => _ = SendTurn();
            }

            uiBuilt = true;
        }

        void EnsureUiBuilt()
        {
            if (uiBuilt && uiDocument != null && uiDocument.rootVisualElement.childCount > 0)
            {
                return;
            }

            BuildUi();
            RefreshTemplateHeader();
        }

        void Update()
        {
            if (!Application.isPlaying || uiDocument == null) return;

            VisualElement root = uiDocument.rootVisualElement;
            if (root == null) return;

            if (root.childCount == 0)
            {
                uiBuilt = false;
                EnsureUiBuilt();
            }
        }

        void RefreshTemplateHeader()
        {
            if (timeManager == null || turnLabel == null) return;
            DateTime sendDate = timeManager.GetSendDate();
            DateTime replyDate = timeManager.GetReplyDate();

            turnLabel.text = $"Turn {timeManager.CurrentTurn + 1} | Reply context: {timeManager.FormatDate(replyDate)}";
            if (toLabel != null) toLabel.text = $"To: {toName}";
            if (fromLabel != null) fromLabel.text = $"From: {fromName}";
            if (dateLabel != null) dateLabel.text = $"Date: {timeManager.FormatDate(sendDate)}";
            if (toAddressLabel != null) toAddressLabel.text = $"Recipient Address: {toAddress}";
            if (fromAddressLabel != null) fromAddressLabel.text = $"Sender Address: {fromAddress}";
        }

        async Task SendTurn()
        {
            if (requestInFlight) return;
            if (llmAgent == null)
            {
                if (statusLabel != null) statusLabel.text = "No LLMAgent found.";
                return;
            }
            if (llmAgent.llm == null || string.IsNullOrWhiteSpace(llmAgent.llm.model))
            {
                if (statusLabel != null) statusLabel.text = "LLM model not configured in LLMStack.";
                return;
            }

            string body = bodyField?.value?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(body))
            {
                if (statusLabel != null) statusLabel.text = "Letter body is empty.";
                return;
            }

            requestInFlight = true;
            sendButton?.SetEnabled(false);
            if (statusLabel != null) statusLabel.text = "Sending letter...";

            DateTime sendDate = timeManager.GetSendDate();
            DateTime replyDate = timeManager.GetReplyDate();

            string playerLetter = ComposePlayerLetter(body, sendDate);
            AddHistoryEntry("Wilmarth", playerLetter);

            llmAgent.systemPrompt = letterPromptBuilder.BuildSystemPrompt(timeManager, lastAssistantLetter);
            string userPrompt = letterPromptBuilder.BuildUserTurnPrompt(timeManager, body);

            StringBuilder stream = new StringBuilder();
            string lastStreamUpdate = "";
            try
            {
                if (statusLabel != null) statusLabel.text = "Receiving reply...";

                await llmAgent.Chat(
                    userPrompt,
                    update =>
                    {
                        if (string.IsNullOrEmpty(update)) return;

                        // Some backends stream token-by-token while others resend the full accumulated text.
                        if (!string.IsNullOrEmpty(lastStreamUpdate) && update.StartsWith(lastStreamUpdate, StringComparison.Ordinal))
                        {
                            stream.Clear();
                            stream.Append(update);
                        }
                        else
                        {
                            stream.Append(update);
                        }

                        lastStreamUpdate = update;
                    }
                );

                lastAssistantLetter = stream.ToString().Trim();
                AddHistoryEntry("Akeley", $"{timeManager.FormatDate(replyDate)}\n\n{lastAssistantLetter}");
                storyEventLedger.RecordGeneratedLetter(replyDate, lastAssistantLetter);
                timeManager.AdvanceTurn();

                if (bodyField != null) bodyField.value = "My dear Mr. Akeley,\n\n";
                RefreshTemplateHeader();
                if (statusLabel != null) statusLabel.text = "Reply received.";
            }
            catch (Exception ex)
            {
                if (statusLabel != null) statusLabel.text = $"Turn failed: {ex.Message}";
            }
            finally
            {
                requestInFlight = false;
                sendButton?.SetEnabled(true);
            }
        }

        string ComposePlayerLetter(string body, DateTime sendDate)
        {
            return
                $"To: {toName}\n" +
                $"From: {fromName}\n" +
                $"Date: {timeManager.FormatDate(sendDate)}\n\n" +
                body + "\n\n" +
                "Yours very truly,\n" +
                fromName;
        }

        Label AddHistoryEntry(string speaker, string content)
        {
            if (historyView == null) return null;

            Label label = new Label
            {
                text = $"{speaker}:\n\n{content}"
            };
            label.AddToClassList("history-entry");
            label.AddToClassList(speaker == "Wilmarth" ? "history-entry-player" : "history-entry-ai");
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 8;
            label.style.paddingLeft = 8;
            label.style.paddingRight = 8;
            label.style.paddingTop = 8;
            label.style.paddingBottom = 8;
            label.style.backgroundColor = speaker == "Wilmarth" ? new Color(0.9f, 0.95f, 1f, 0.7f) : new Color(1f, 0.95f, 0.85f, 0.7f);
            historyView.Add(label);
            historyView.scrollOffset = new Vector2(0, historyView.contentContainer.layout.height + 9999f);
            return label;
        }
    }
}