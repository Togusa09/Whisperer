using UnityEngine;
using UnityEngine.UIElements;

namespace Whisperer
{
    [RequireComponent(typeof(UIDocument))]
    public class InteractionHudController : MonoBehaviour
    {
        const string LayoutResourcePath = "Whisperer/UI/InteractionHud";
        const string StyleResourcePath = "Whisperer/UI/InteractionHud";
        const string PanelSettingsResourcePath = "Whisperer/UI/InteractionHudPanelSettings";

        [SerializeField] PanelSettings panelSettingsAsset;

        UIDocument uiDocument;
        VisualTreeAsset layoutAsset;
        StyleSheet styleSheet;
        PanelSettings runtimePanelSettings;
        VisualElement reticle;
        Label promptLabel;
        bool uiBuilt;

        void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            ResolveDependencies();
        }

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureUiBuilt();
        }

        void EnsureUiBuilt()
        {
            if (uiBuilt)
            {
                return;
            }

            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            ResolveDependencies();

            if (uiDocument == null)
            {
                return;
            }

            VisualElement root = uiDocument.rootVisualElement;
            if (root == null)
            {
                return;
            }

            root.Clear();

            if (layoutAsset == null)
            {
                Debug.LogWarning($"InteractionHudController: UXML not found at Resources/{LayoutResourcePath}.", this);
                return;
            }

            layoutAsset.CloneTree(root);
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            reticle = root.Q("Reticle");
            promptLabel = root.Q<Label>("InteractionPrompt");

            if (reticle != null)
            {
                reticle.pickingMode = PickingMode.Ignore;
            }

            if (promptLabel != null)
            {
                promptLabel.pickingMode = PickingMode.Ignore;
                promptLabel.style.display = DisplayStyle.None;
            }

            VisualElement hudRoot = root.Q("HudRoot");
            if (hudRoot != null)
            {
                hudRoot.pickingMode = PickingMode.Ignore;
            }

            ApplyFallbackStyles(root, hudRoot);

            uiBuilt = true;
        }

        void ApplyFallbackStyles(VisualElement root, VisualElement hudRoot)
        {
            if (root != null)
            {
                root.pickingMode = PickingMode.Ignore;
            }

            if (hudRoot != null)
            {
                hudRoot.style.position = Position.Absolute;
                hudRoot.style.left = 0f;
                hudRoot.style.top = 0f;
                hudRoot.style.right = 0f;
                hudRoot.style.bottom = 0f;
                hudRoot.style.alignItems = Align.Center;
                hudRoot.style.justifyContent = Justify.Center;
                hudRoot.style.flexDirection = FlexDirection.Column;
                hudRoot.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }

            if (reticle != null)
            {
                reticle.style.position = Position.Absolute;
                reticle.style.left = new Length(50f, LengthUnit.Percent);
                reticle.style.top = new Length(50f, LengthUnit.Percent);
                reticle.style.marginLeft = -4f;
                reticle.style.marginTop = -4f;
                reticle.style.width = 8f;
                reticle.style.height = 8f;
                reticle.style.backgroundColor = new Color(1f, 1f, 1f, 0.3f);
            }

            if (promptLabel != null)
            {
                promptLabel.style.marginTop = 40f;
                promptLabel.style.color = new Color(1f, 1f, 1f, 0.9f);
                promptLabel.style.fontSize = 14f;
                promptLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                promptLabel.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
                promptLabel.style.borderTopLeftRadius = 4f;
                promptLabel.style.borderTopRightRadius = 4f;
                promptLabel.style.borderBottomLeftRadius = 4f;
                promptLabel.style.borderBottomRightRadius = 4f;
                promptLabel.style.paddingTop = 4f;
                promptLabel.style.paddingBottom = 4f;
                promptLabel.style.paddingLeft = 8f;
                promptLabel.style.paddingRight = 8f;
            }
        }

        void ResolveDependencies()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument != null && uiDocument.panelSettings == null)
            {
                uiDocument.panelSettings = ResolvePanelSettings();
            }

            if (layoutAsset == null)
            {
                layoutAsset = Resources.Load<VisualTreeAsset>(LayoutResourcePath);
            }

            if (styleSheet == null)
            {
                styleSheet = Resources.Load<StyleSheet>(StyleResourcePath);
            }
        }

        PanelSettings ResolvePanelSettings()
        {
            if (panelSettingsAsset != null)
            {
                return panelSettingsAsset;
            }

            panelSettingsAsset = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
            if (panelSettingsAsset != null)
            {
                return panelSettingsAsset;
            }

            UIDocument existingDocument = FindAnyObjectByType<UIDocument>();
            if (existingDocument != null && existingDocument != uiDocument && existingDocument.panelSettings != null)
            {
                panelSettingsAsset = existingDocument.panelSettings;
                return panelSettingsAsset;
            }

            PanelSettings[] allPanelSettings = Resources.FindObjectsOfTypeAll<PanelSettings>();
            if (allPanelSettings != null)
            {
                for (int i = 0; i < allPanelSettings.Length; i += 1)
                {
                    if (allPanelSettings[i] != null)
                    {
                        panelSettingsAsset = allPanelSettings[i];
                        return panelSettingsAsset;
                    }
                }
            }

            if (runtimePanelSettings == null)
            {
                runtimePanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                runtimePanelSettings.name = "InteractionHudPanelSettings";
                runtimePanelSettings.hideFlags = HideFlags.DontSave;
            }

            return runtimePanelSettings;
        }

        void OnDestroy()
        {
            if (runtimePanelSettings != null)
            {
                Destroy(runtimePanelSettings);
                runtimePanelSettings = null;
            }
        }

        public void SetState(bool hovering, string prompt)
        {
            if (!uiBuilt)
            {
                EnsureUiBuilt();
            }

            if (reticle == null)
            {
                return;
            }

            reticle.EnableInClassList("reticle--hover", hovering);
            reticle.style.backgroundColor = hovering
                ? new Color(0.95f, 0.83f, 0.35f, 0.95f)
                : new Color(1f, 1f, 1f, 0.3f);

            if (promptLabel != null)
            {
                bool hasPrompt = !string.IsNullOrEmpty(prompt);
                promptLabel.style.display = hasPrompt ? DisplayStyle.Flex : DisplayStyle.None;
                promptLabel.text = hasPrompt ? prompt : "";
            }
        }

        public void SetVisible(bool visible)
        {
            if (!uiBuilt)
            {
                EnsureUiBuilt();
            }

            VisualElement root = uiDocument?.rootVisualElement;
            if (root != null)
            {
                root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
