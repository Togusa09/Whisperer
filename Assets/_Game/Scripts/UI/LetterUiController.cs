using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMUnity;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace Whisperer
{
    public abstract class LetterModelClient : MonoBehaviour
    {
        public abstract bool IsConfigured { get; }
        public abstract Task<string> GenerateReply(string systemPrompt, string userPrompt, Action<string> onUpdate);
    }

    public class LlmAgentLetterModelClient : LetterModelClient
    {
        public LLMAgent llmAgent;

        public override bool IsConfigured =>
            llmAgent != null && llmAgent.llm != null && !string.IsNullOrWhiteSpace(llmAgent.llm.model);

        public override async Task<string> GenerateReply(string systemPrompt, string userPrompt, Action<string> onUpdate)
        {
            if (!IsConfigured) throw new InvalidOperationException("LLM model not configured in LLMStack.");

            llmAgent.systemPrompt = systemPrompt;
            StringBuilder stream = new StringBuilder();
            string lastStreamUpdate = "";

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
                    onUpdate?.Invoke(stream.ToString());
                }
            );

            return stream.ToString();
        }
    }

    [RequireComponent(typeof(UIDocument))]
    public class LetterUiController : MonoBehaviour
    {
        class TurnArchiveRecord
        {
            public int turnIndex;
            public string senderName;
            public DateTime sendDate;
            public DateTime replyDate;
            public string playerLetter;
            public string assistantLetter;
        }

        const string LayoutResourcePath = "Whisperer/UI/LetterUI";
        const string StyleResourcePath = "Whisperer/UI/LetterUITheme";

        [Header("References")]
        public LLMAgent llmAgent;
        public LetterModelClient modelClient;
        public GameTimeManager timeManager;
        public StoryEventLedger storyEventLedger;
        public LetterPromptBuilder letterPromptBuilder;
        public StoryConsistencyValidator consistencyValidator;
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
        Label sanityLabel;
        Label trustLabel;
        Label toAddressLabel;
        Label fromAddressLabel;
        TextField bodyField;
        Button sendButton;
        Label statusLabel;
        ScrollView archiveListView;
        ScrollView archiveDetailView;
        Label archiveDetailLabel;
        Label diagnosticsSummaryLabel;
        Label diagnosticsModelLabel;
        Label diagnosticsResourceLabel;
        Label diagnosticsPromptLabel;
        Label diagnosticsResponseLabel;
        Label diagnosticsValidationLabel;
        Button refreshDiagnosticsButton;
        Button pauseModelButton;
        Button stopGenerationButton;
        Button clearContextButton;
        Button openDiagnosticsButton;
        Button diagnosticsCloseButton;
        VisualElement diagnosticsOverlay;
        VisualElement letterPopupOverlay;
        Label popupLetterContent;
        Button popupCloseButton;

        bool requestInFlight;
        bool uiBuilt;
        string lastAssistantLetter = "";
        VisualTreeAsset layoutAsset;
        StyleSheet styleSheet;
        readonly List<TurnArchiveRecord> turnArchive = new List<TurnArchiveRecord>();
        int akeleySanity = 70;
        int akeleyTrust = 50;
        bool llmPaused;
        float nextDiagnosticsRefreshTime;
        long lastGenerationMs;
        int lastPromptChars;
        int lastResponseChars;
        bool lastFallbackUsed;
        string lastValidationReport = "";
        string lastSystemPromptSent = "";
        string lastUserPromptSent = "";

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
            if (modelClient == null) modelClient = GetComponent<LetterModelClient>();
            if (timeManager == null) timeManager = FindAnyObjectByType<GameTimeManager>();
            if (storyEventLedger == null) storyEventLedger = FindAnyObjectByType<StoryEventLedger>();
            if (letterPromptBuilder == null) letterPromptBuilder = FindAnyObjectByType<LetterPromptBuilder>();
            if (consistencyValidator == null) consistencyValidator = FindAnyObjectByType<StoryConsistencyValidator>();

            if (timeManager == null) timeManager = gameObject.AddComponent<GameTimeManager>();
            if (storyEventLedger == null) storyEventLedger = gameObject.AddComponent<StoryEventLedger>();
            if (letterPromptBuilder == null) letterPromptBuilder = gameObject.AddComponent<LetterPromptBuilder>();
            if (consistencyValidator == null) consistencyValidator = gameObject.AddComponent<StoryConsistencyValidator>();

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
            sanityLabel = root.Q<Label>("SanityLabel");
            trustLabel = root.Q<Label>("TrustLabel");
            toAddressLabel = root.Q<Label>("ToAddressLabel");
            fromAddressLabel = root.Q<Label>("FromAddressLabel");
            bodyField = root.Q<TextField>("BodyField");
            sendButton = root.Q<Button>("SendButton");
            statusLabel = root.Q<Label>("StatusLabel");
            archiveListView = root.Q<ScrollView>("ArchiveListView");
            archiveDetailView = root.Q<ScrollView>("ArchiveDetailView");
            archiveDetailLabel = root.Q<Label>("ArchiveDetailLabel");
            diagnosticsSummaryLabel = root.Q<Label>("DiagnosticsSummaryLabel");
            diagnosticsModelLabel = root.Q<Label>("DiagnosticsModelLabel");
            diagnosticsResourceLabel = root.Q<Label>("DiagnosticsResourceLabel");
            diagnosticsPromptLabel = root.Q<Label>("DiagnosticsPromptLabel");
            diagnosticsResponseLabel = root.Q<Label>("DiagnosticsResponseLabel");
            diagnosticsValidationLabel = root.Q<Label>("DiagnosticsValidationLabel");
            refreshDiagnosticsButton = root.Q<Button>("RefreshDiagnosticsButton");
            pauseModelButton = root.Q<Button>("PauseModelButton");
            stopGenerationButton = root.Q<Button>("StopGenerationButton");
            clearContextButton = root.Q<Button>("ClearContextButton");
            openDiagnosticsButton = root.Q<Button>("OpenDiagnosticsButton");
            diagnosticsCloseButton = root.Q<Button>("DiagnosticsCloseButton");
            diagnosticsOverlay = root.Q<VisualElement>("DiagnosticsOverlay");
            letterPopupOverlay = root.Q<VisualElement>("LetterPopupOverlay");
            popupLetterContent = root.Q<Label>("PopupLetterContent");
            popupCloseButton = root.Q<Button>("PopupCloseButton");

            if (bodyField != null)
            {
                bodyField.multiline = true;
                bodyField.value = "My dear Mr. Akeley,\n\n";
            }

            if (sendButton != null)
            {
                sendButton.clicked += () => _ = SendTurn();
            }

            if (popupCloseButton != null)
            {
                popupCloseButton.clicked += CloseLetterPopup;
            }

            if (refreshDiagnosticsButton != null)
            {
                refreshDiagnosticsButton.clicked += () => RefreshDiagnostics(true);
            }

            if (pauseModelButton != null)
            {
                pauseModelButton.clicked += ToggleLlmPaused;
                pauseModelButton.text = "Pause LLM";
            }

            if (stopGenerationButton != null)
            {
                stopGenerationButton.clicked += StopGeneration;
            }

            if (clearContextButton != null)
            {
                clearContextButton.clicked += ClearModelContext;
            }

            if (openDiagnosticsButton != null)
            {
                openDiagnosticsButton.clicked += ShowDiagnosticsDialog;
            }

            if (diagnosticsCloseButton != null)
            {
                diagnosticsCloseButton.clicked += CloseDiagnosticsDialog;
            }

            if (diagnosticsOverlay != null)
            {
                diagnosticsOverlay.style.display = DisplayStyle.None;
            }

            uiBuilt = true;
            InitializeWithSeedCorrespondence();
            RefreshArchiveUi();
            RefreshDiagnostics(true);
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

            if (Time.unscaledTime >= nextDiagnosticsRefreshTime)
            {
                RefreshDiagnostics(false);
                nextDiagnosticsRefreshTime = Time.unscaledTime + 1f;
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
            if (sanityLabel != null) sanityLabel.text = $"Akeley Stability: {akeleySanity}/100 ({DescribeSanityState()})";
            if (trustLabel != null) trustLabel.text = $"Akeley Trust: {akeleyTrust}/100 ({DescribeTrustState()})";
            if (toAddressLabel != null) toAddressLabel.text = $"Recipient Address: {toAddress}";
            if (fromAddressLabel != null) fromAddressLabel.text = $"Sender Address: {fromAddress}";
        }

        string DescribeSanityState()
        {
            if (akeleySanity >= 75) return "Steady";
            if (akeleySanity >= 45) return "Strained";
            return "Fraying";
        }

        string DescribeTrustState()
        {
            if (akeleyTrust >= 75) return "Confiding";
            if (akeleyTrust >= 45) return "Cautious";
            return "Guarded";
        }

        void ApplyMvpRelationshipEffects(string playerBody)
        {
            string text = (playerBody ?? "").ToLowerInvariant();
            int sanityDelta = -1;
            int trustDelta = 0;

            string[] supportive = { "believe", "trust", "help", "concern", "care", "friend", "support", "safely", "protect" };
            string[] dismissive = { "nonsense", "absurd", "delusion", "insane", "madness", "hysteria", "fabricat", "liar" };
            string[] practical = { "evidence", "detail", "record", "note", "witness", "investigate", "authorit", "sheriff", "photograph", "sample" };

            int supportiveHits = CountKeywordHits(text, supportive);
            int dismissiveHits = CountKeywordHits(text, dismissive);
            int practicalHits = CountKeywordHits(text, practical);

            trustDelta += Mathf.Min(6, supportiveHits * 2);
            sanityDelta += Mathf.Min(4, supportiveHits);

            trustDelta -= Mathf.Min(8, dismissiveHits * 3);
            sanityDelta -= Mathf.Min(6, dismissiveHits * 2);

            trustDelta += Mathf.Min(6, practicalHits * 2);
            sanityDelta += Mathf.Min(3, practicalHits);

            if (text.Length > 280) trustDelta += 1;
            if (text.Length < 80) trustDelta -= 1;

            akeleySanity = Mathf.Clamp(akeleySanity + sanityDelta, 0, 100);
            akeleyTrust = Mathf.Clamp(akeleyTrust + trustDelta, 0, 100);
        }

        static int CountKeywordHits(string text, string[] keywords)
        {
            int hits = 0;
            for (int i = 0; i < keywords.Length; i++)
            {
                if (text.Contains(keywords[i])) hits++;
            }
            return hits;
        }

        async Task SendTurn()
        {
            await SendTurnInternal(null);
        }

        async Task SendTurnInternal(string bodyOverride)
        {
            if (requestInFlight) return;
            if (llmPaused)
            {
                if (statusLabel != null) statusLabel.text = "LLM is paused.";
                return;
            }

            LetterModelClient activeModelClient = ResolveModelClient();
            if (activeModelClient == null)
            {
                if (statusLabel != null) statusLabel.text = "No model client found.";
                return;
            }

            if (!activeModelClient.IsConfigured)
            {
                if (statusLabel != null) statusLabel.text = "LLM model not configured in LLMStack.";
                return;
            }

            string body = bodyOverride ?? bodyField?.value?.Trim() ?? "";
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

            ApplyMvpRelationshipEffects(body);
            if (letterPromptBuilder != null)
            {
                letterPromptBuilder.SetRelationshipState(akeleySanity, akeleyTrust);
            }

            string systemPrompt = letterPromptBuilder.BuildSystemPrompt(timeManager, lastAssistantLetter);
            string userPrompt = letterPromptBuilder.BuildUserTurnPrompt(timeManager, body);
            lastSystemPromptSent = systemPrompt;
            lastUserPromptSent = userPrompt;
            lastPromptChars = (systemPrompt?.Length ?? 0) + (userPrompt?.Length ?? 0);
            long generationStartTicks = DateTime.UtcNow.Ticks;
            lastFallbackUsed = false;
            lastValidationReport = "Validation not run.";

            try
            {
                if (statusLabel != null) statusLabel.text = "Receiving reply...";

                string assistantReply = await activeModelClient.GenerateReply(systemPrompt, userPrompt, _ => { });
                string candidateReply = assistantReply?.Trim() ?? "";

                if (consistencyValidator != null)
                {
                    StoryConsistencyValidator.ValidationResult validation = consistencyValidator.ValidateDraft(
                        replyDate,
                        timeManager.knowledgeCutoffYear,
                        candidateReply,
                        storyEventLedger);

                    if (!validation.isConsistent)
                    {
                        lastFallbackUsed = true;
                        if (statusLabel != null) statusLabel.text = "Consistency check failed, generating corrected draft...";
                        string fallbackPrompt = letterPromptBuilder.BuildConsistencyFallbackPrompt(timeManager, body, candidateReply, validation.report);
                        string correctedReply = await activeModelClient.GenerateReply(systemPrompt, fallbackPrompt, _ => { });
                        candidateReply = correctedReply?.Trim() ?? candidateReply;
                    }

                    lastValidationReport = validation.report;
                }

                lastAssistantLetter = candidateReply;
                lastResponseChars = lastAssistantLetter.Length;
                lastGenerationMs = (DateTime.UtcNow.Ticks - generationStartTicks) / TimeSpan.TicksPerMillisecond;
                UpdateReceivedLetterView(replyDate, lastAssistantLetter);
                storyEventLedger.RecordGeneratedLetter(replyDate, lastAssistantLetter);
                RecordArchiveTurn(sendDate, replyDate, playerLetter, lastAssistantLetter);
                timeManager.AdvanceTurn();

                if (bodyField != null) bodyField.value = "My dear Mr. Akeley,\n\n";
                RefreshTemplateHeader();
                RefreshDiagnostics(true);
                if (statusLabel != null) statusLabel.text = "Reply received.";
            }
            catch (Exception ex)
            {
                lastValidationReport = $"Generation failed: {ex.Message}";
                RefreshDiagnostics(true);
                if (statusLabel != null) statusLabel.text = $"Turn failed: {ex.Message}";
            }
            finally
            {
                requestInFlight = false;
                sendButton?.SetEnabled(!llmPaused);
            }
        }

        void ToggleLlmPaused()
        {
            llmPaused = !llmPaused;
            if (pauseModelButton != null)
            {
                pauseModelButton.text = llmPaused ? "Resume LLM" : "Pause LLM";
            }

            if (!requestInFlight)
            {
                sendButton?.SetEnabled(!llmPaused);
            }

            if (statusLabel != null)
            {
                statusLabel.text = llmPaused ? "LLM paused." : "LLM resumed.";
            }

            RefreshDiagnostics(true);
        }

        void StopGeneration()
        {
            llmAgent?.CancelRequests();
            if (statusLabel != null)
            {
                statusLabel.text = "Stop requested.";
            }

            RefreshDiagnostics(true);
        }

        void ShowDiagnosticsDialog()
        {
            if (diagnosticsOverlay == null) return;
            diagnosticsOverlay.style.display = DisplayStyle.Flex;
            RefreshDiagnostics(true);
        }

        void CloseDiagnosticsDialog()
        {
            if (diagnosticsOverlay == null) return;
            diagnosticsOverlay.style.display = DisplayStyle.None;
        }

        void ClearModelContext()
        {
            int before = 0;
            int after = 0;

            if (llmAgent != null && llmAgent.chat != null)
            {
                before = llmAgent.chat.Count;
                if (before > 0)
                {
                    var firstMessage = llmAgent.chat[0];
                    llmAgent.chat.Clear();
                    llmAgent.chat.Add(firstMessage);
                }
                else
                {
                    llmAgent.chat.Clear();
                }

                after = llmAgent.chat.Count;
            }

            if (statusLabel != null)
            {
                statusLabel.text = $"Model context cleared ({before} -> {after} messages).";
            }

            RefreshDiagnostics(true);
        }

        void RefreshDiagnostics(bool includeVerbose)
        {
            string modelName = llmAgent != null && llmAgent.llm != null ? llmAgent.llm.model : "not configured";
            int chatCount = llmAgent != null && llmAgent.chat != null ? llmAgent.chat.Count : 0;
            long managedBytes = GC.GetTotalMemory(false);
            long monoBytes = Profiler.GetMonoUsedSizeLong();
            long totalAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong();

            if (diagnosticsSummaryLabel != null)
            {
                diagnosticsSummaryLabel.text =
                    $"Summary: paused={llmPaused}, inFlight={requestInFlight}, fallback={lastFallbackUsed}, genMs={lastGenerationMs}";
            }

            if (diagnosticsModelLabel != null)
            {
                diagnosticsModelLabel.text =
                    $"Model: {modelName} | chatMessages={chatCount} | promptChars={lastPromptChars} | responseChars={lastResponseChars}";
            }

            if (diagnosticsResourceLabel != null)
            {
                diagnosticsResourceLabel.text =
                    $"Resources: managed={FormatMegabytes(managedBytes)}MB, mono={FormatMegabytes(monoBytes)}MB, allocated={FormatMegabytes(totalAllocatedBytes)}MB";
            }

            if (includeVerbose && diagnosticsPromptLabel != null)
            {
                string promptPreview =
                    "SYSTEM:\n" + Truncate(lastSystemPromptSent, 900) +
                    "\n\nUSER:\n" + Truncate(lastUserPromptSent, 900);
                diagnosticsPromptLabel.text = "Last prompt:\n" + promptPreview;
            }

            if (includeVerbose && diagnosticsResponseLabel != null)
            {
                diagnosticsResponseLabel.text = "Last response:\n" + Truncate(lastAssistantLetter, 1300);
            }

            if (diagnosticsValidationLabel != null)
            {
                string validationText = consistencyValidator != null
                    ? consistencyValidator.LastReport
                    : lastValidationReport;
                diagnosticsValidationLabel.text = "Consistency:\n" + Truncate(validationText, 1200);
            }
        }

        static string FormatMegabytes(long bytes)
        {
            double mb = bytes / (1024d * 1024d);
            return mb.ToString("0.0");
        }

        static string Truncate(string text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text)) return "n/a";
            if (text.Length <= maxChars) return text;
            return text.Substring(0, maxChars) + "\n...[truncated]";
        }

        LetterModelClient ResolveModelClient()
        {
            if (modelClient != null) return modelClient;

            if (llmAgent != null)
            {
                LlmAgentLetterModelClient adapter = gameObject.GetComponent<LlmAgentLetterModelClient>();
                if (adapter == null)
                {
                    adapter = gameObject.AddComponent<LlmAgentLetterModelClient>();
                }

                adapter.llmAgent = llmAgent;
                modelClient = adapter;
            }

            return modelClient;
        }

        public Task SendTurnForTests(string letterBody)
        {
            return SendTurnInternal(letterBody);
        }

        public string StatusTextForTests => statusLabel?.text ?? "";

        public int ArchiveTurnCountForTests => turnArchive.Count;

        public string ArchiveDetailTextForTests => archiveDetailLabel?.text ?? "";

        void UpdateReceivedLetterView(DateTime replyDate, string letterContent)
        {
            if (popupLetterContent == null) return;
            popupLetterContent.text = 
                $"{timeManager.FormatDate(replyDate)}\n\n" +
                letterContent;
            ShowLetterPopup();
        }

        void ShowLetterPopup()
        {
            if (letterPopupOverlay == null) return;
            letterPopupOverlay.style.display = DisplayStyle.Flex;
        }

        void CloseLetterPopup()
        {
            if (letterPopupOverlay == null) return;
            letterPopupOverlay.style.display = DisplayStyle.None;
        }

        void InitializeWithSeedCorrespondence()
        {
            // Add Akeley's introduction letter from the story
            string akeleyIntro = "My dear Mr. Wilmarth,\n\n" +
                "I have ventured to write you after reading your discourse in the Arkham Advertiser regarding the flood phenomena and the ancient Vermont legends. " +
                "Your skepticism is perhaps warranted, but I believe you to be a man of both learning and intellectual honesty, and I felt compelled to share observations " +
                "I have made from my farm here in the hills.\n\n" +
                "The things that the rural folk speak of are all too real, I fear. I have found evidence of peculiar tracks and formations in the remote valleys near my property. " +
                "More troubling still are the sounds—strange mutterings in the night that seem almost articulate, yet utterly alien.\n\n" +
                "I hesitate to trouble you with these matters without stronger proof, but I find myself increasingly convinced that the legends contain a kernel of terrible truth. " +
                "Should you be willing to correspond further on this matter, I would welcome your thoughts on the historical record of these sightings.\n\n" +
                "Yours in cautious inquiry,\n" +
                "Henry Wentworth Akeley";
            
            DateTime akeleyDate = new DateTime(1928, 4, 1);
            turnArchive.Add(new TurnArchiveRecord
            {
                turnIndex = 0,
                senderName = "Henry W. Akeley",
                sendDate = akeleyDate,
                replyDate = akeleyDate,
                playerLetter = "",
                assistantLetter = akeleyIntro
            });

            // Add initial flood creature context letters
            string floodContextA = "My dear Mr. Wilmarth,\n\n" +
                "I thought you might be interested in hearing of some unusual reports circulating among my colleagues here at the University. " +
                "During the recent floods in November, several farmers and local observers claim to have witnessed curious objects in the swollen waters.\n\n" +
                "The descriptions are remarkably consistent—creatures of some sort, though their exact nature remains unclear. " +
                "Some witnesses spoke of organic shapes unlike any known animal, with peculiar appendages and an unsettling appearance.\n\n" +
                "The general conclusion among my peers is that these are merely misidentified flood debris or animal remains, distorted by imagination and folklore. " +
                "However, I find the consistency of the reports intriguing. Thought you should know of these accounts.\n\n" +
                "Best regards,\n" +
                "A Colleague at Miskatonic University";

            DateTime floodDate1 = new DateTime(1927, 11, 15);
            turnArchive.Add(new TurnArchiveRecord
            {
                turnIndex = 0,
                senderName = "A Colleague at Miskatonic University",
                sendDate = floodDate1,
                replyDate = floodDate1,
                playerLetter = "",
                assistantLetter = floodContextA
            });

            string floodContextB = "My dear Mr. Wilmarth,\n\n" +
                "Further to my previous letter regarding the flood phenomena, I have learned of additional accounts from the river valleys. " +
                "A farmer near Newfane reported finding peculiar impressions in the muddy riverbank—marks that do not match any known animal.\n\n" +
                "The local folk are naturally inclined toward supernatural explanations, resurrecting old legends about the 'creatures of the hills.' " +
                "Naturally, we must be skeptical of such claims, yet the physical evidence deserves examination.\n\n" +
                "I confess I am uncertain what to make of these reports. Perhaps you might offer your scholarly perspective on the historical precedent for such sightings.\n\n" +
                "Your friend,\n" +
                "A Vermont Correspondent";

            DateTime floodDate2 = new DateTime(1927, 11, 25);
            turnArchive.Add(new TurnArchiveRecord
            {
                turnIndex = 0,
                senderName = "A Vermont Correspondent",
                sendDate = floodDate2,
                replyDate = floodDate2,
                playerLetter = "",
                assistantLetter = floodContextB
            });
        }

        void RecordArchiveTurn(DateTime sendDate, DateTime replyDate, string playerLetter, string assistantLetter)
        {
            turnArchive.Add(new TurnArchiveRecord
            {
                turnIndex = timeManager.CurrentTurn + 1,
                senderName = toName,
                sendDate = sendDate,
                replyDate = replyDate,
                playerLetter = playerLetter,
                assistantLetter = assistantLetter
            });

            RefreshArchiveUi();
        }

        void RefreshArchiveUi()
        {
            if (archiveListView == null || archiveDetailLabel == null) return;

            archiveListView.Clear();
            if (turnArchive.Count == 0)
            {
                archiveDetailLabel.text = "No turns recorded yet.";
                return;
            }

            List<TurnArchiveRecord> sorted = turnArchive.OrderBy(r => r.sendDate).ToList();
            List<Button> buttons = new List<Button>();
            for (int i = 0; i < sorted.Count; i++)
            {
                TurnArchiveRecord record = sorted[i];
                int index = i;
                string label = string.IsNullOrWhiteSpace(record.senderName)
                    ? record.sendDate.ToString("MMM d, yyyy")
                    : $"{record.sendDate:MMM d, yyyy} \u2014 {record.senderName}";
                Button button = new Button(() =>
                {
                    foreach (Button b in buttons)
                        b.RemoveFromClassList("archive-turn-button--selected");
                    buttons[index].AddToClassList("archive-turn-button--selected");
                    ShowArchiveTurnFromSorted(sorted, index);
                })
                {
                    text = label
                };
                button.AddToClassList("archive-turn-button");
                archiveListView.Add(button);
                buttons.Add(button);
            }

            int lastIndex = sorted.Count - 1;
            buttons[lastIndex].AddToClassList("archive-turn-button--selected");
            ShowArchiveTurnFromSorted(sorted, lastIndex);
        }

        void ShowArchiveTurn(int index)
        {
            if (archiveDetailLabel == null) return;
            if (index < 0 || index >= turnArchive.Count) return;
            ShowArchiveTurnRecord(turnArchive[index]);
        }

        void ShowArchiveTurnFromSorted(List<TurnArchiveRecord> sorted, int index)
        {
            if (archiveDetailLabel == null) return;
            if (index < 0 || index >= sorted.Count) return;
            ShowArchiveTurnRecord(sorted[index]);
        }

        void ShowArchiveTurnRecord(TurnArchiveRecord record)
        {
            var sb = new StringBuilder();
            sb.AppendLine(timeManager.FormatDate(record.sendDate));
            if (!string.IsNullOrWhiteSpace(record.playerLetter))
            {
                sb.AppendLine();
                sb.AppendLine("Wilmarth:");
                sb.AppendLine(record.playerLetter);
            }
            sb.AppendLine();
            string sender = string.IsNullOrWhiteSpace(record.senderName) ? "Akeley" : record.senderName;
            sb.AppendLine($"{sender}:");
            sb.Append(record.assistantLetter);
            archiveDetailLabel.text = sb.ToString();
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
    }
}