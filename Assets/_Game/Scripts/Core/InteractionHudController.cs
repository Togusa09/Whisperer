using UnityEngine;
using UnityEngine.UIElements;

namespace Whisperer
{
    [RequireComponent(typeof(UIDocument))]
    public class InteractionHudController : MonoBehaviour
    {
        const string LayoutResourcePath = "Whisperer/UI/InteractionHud";
        const string StyleResourcePath = "Whisperer/UI/InteractionHud";

        [SerializeField] PanelSettings panelSettingsAsset;

        UIDocument uiDocument;
        VisualElement reticle;
        Label promptLabel;
        bool uiBuilt;

        void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument.panelSettings == null && panelSettingsAsset != null)
            {
                uiDocument.panelSettings = panelSettingsAsset;
            }
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

            VisualTreeAsset layout = Resources.Load<VisualTreeAsset>(LayoutResourcePath);
            if (layout == null)
            {
                Debug.LogWarning($"InteractionHudController: UXML not found at Resources/{LayoutResourcePath}.", this);
                return;
            }

            uiDocument.visualTreeAsset = layout;

            StyleSheet styleSheet = Resources.Load<StyleSheet>(StyleResourcePath);
            if (styleSheet != null)
            {
                uiDocument.rootVisualElement?.styleSheets.Add(styleSheet);
            }

            VisualElement root = uiDocument.rootVisualElement;
            if (root == null)
            {
                return;
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

            uiBuilt = true;
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
