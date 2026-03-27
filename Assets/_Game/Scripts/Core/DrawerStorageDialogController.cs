using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Whisperer
{
    [RequireComponent(typeof(UIDocument))]
    public class DrawerStorageDialogController : MonoBehaviour
    {
        const string PanelSettingsResourcePath = "Whisperer/UI/InteractionHudPanelSettings";

        [SerializeField] PanelSettings panelSettingsAsset;

        UIDocument uiDocument;
        PanelSettings runtimePanelSettings;
        VisualElement overlay;
        Label titleLabel;
        Label hintLabel;
        Label emptyLabel;
        ScrollView itemList;
        bool uiBuilt;
        bool cursorOverrideActive;
        Func<CarriableItem, bool> selectHandler;
        Action dismissHandler;

        public bool IsOpen { get; private set; }

        public static DrawerStorageDialogController GetOrCreate()
        {
            DrawerStorageDialogController existing = FindAnyObjectByType<DrawerStorageDialogController>();
            if (existing != null)
            {
                return existing;
            }

            GameObject dialogObject = new GameObject("DrawerStorageDialog");
            UIDocument document = dialogObject.AddComponent<UIDocument>();
            DrawerStorageDialogController controller = dialogObject.AddComponent<DrawerStorageDialogController>();
            document.sortingOrder = short.MaxValue - 1;
            return controller;
        }

        void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            ResolvePanelSettings();
            EnsureUiBuilt();
            HideImmediate();
        }

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResolvePanelSettings();
            EnsureUiBuilt();
        }

        void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Dismiss();
            }
        }

        public void Show(string drawerLabel, IReadOnlyList<CarriableItem> items, Func<CarriableItem, bool> onSelect, Action onDismiss)
        {
            EnsureUiBuilt();

            selectHandler = onSelect;
            dismissHandler = onDismiss;
            titleLabel.text = string.IsNullOrWhiteSpace(drawerLabel) ? "Desk Drawer" : drawerLabel;
            hintLabel.text = "Select an item to take out, or close the drawer.";
            PopulateItemButtons(items);

            if (!cursorOverrideActive)
            {
                GameCursorController.PushModalUi();
                cursorOverrideActive = true;
            }

            overlay.style.display = DisplayStyle.Flex;
            IsOpen = true;
        }

        public void Dismiss()
        {
            Close(invokeDismiss: true);
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

            ResolvePanelSettings();

            VisualElement root = uiDocument.rootVisualElement;
            root.Clear();

            overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0f;
            overlay.style.top = 0f;
            overlay.style.right = 0f;
            overlay.style.bottom = 0f;
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.3f);

            VisualElement panel = new VisualElement();
            panel.style.width = 360f;
            panel.style.maxHeight = 420f;
            panel.style.paddingLeft = 18f;
            panel.style.paddingRight = 18f;
            panel.style.paddingTop = 16f;
            panel.style.paddingBottom = 16f;
            panel.style.backgroundColor = new Color(0.12f, 0.09f, 0.06f, 0.96f);
            panel.style.borderTopLeftRadius = 8f;
            panel.style.borderTopRightRadius = 8f;
            panel.style.borderBottomLeftRadius = 8f;
            panel.style.borderBottomRightRadius = 8f;
            panel.style.borderLeftWidth = 1f;
            panel.style.borderRightWidth = 1f;
            panel.style.borderTopWidth = 1f;
            panel.style.borderBottomWidth = 1f;
            panel.style.borderLeftColor = new Color(0.76f, 0.66f, 0.42f, 0.5f);
            panel.style.borderRightColor = new Color(0.76f, 0.66f, 0.42f, 0.5f);
            panel.style.borderTopColor = new Color(0.76f, 0.66f, 0.42f, 0.5f);
            panel.style.borderBottomColor = new Color(0.76f, 0.66f, 0.42f, 0.5f);

            titleLabel = new Label();
            titleLabel.style.color = new Color(0.93f, 0.87f, 0.73f, 1f);
            titleLabel.style.fontSize = 18f;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            hintLabel = new Label();
            hintLabel.style.marginTop = 6f;
            hintLabel.style.marginBottom = 12f;
            hintLabel.style.color = new Color(0.86f, 0.82f, 0.74f, 0.9f);
            hintLabel.style.whiteSpace = WhiteSpace.Normal;

            itemList = new ScrollView(ScrollViewMode.Vertical);
            itemList.style.flexGrow = 1f;
            itemList.style.maxHeight = 260f;

            emptyLabel = new Label("This drawer is empty.");
            emptyLabel.style.marginTop = 4f;
            emptyLabel.style.marginBottom = 12f;
            emptyLabel.style.color = new Color(0.86f, 0.82f, 0.74f, 0.9f);

            Button closeButton = new Button(Dismiss)
            {
                text = "Close Drawer"
            };
            closeButton.style.marginTop = 14f;
            StyleActionButton(closeButton);

            panel.Add(titleLabel);
            panel.Add(hintLabel);
            panel.Add(itemList);
            panel.Add(emptyLabel);
            panel.Add(closeButton);
            overlay.Add(panel);
            root.Add(overlay);

            uiBuilt = true;
        }

        void PopulateItemButtons(IReadOnlyList<CarriableItem> items)
        {
            itemList.Clear();

            int visibleItemCount = 0;
            if (items != null)
            {
                for (int i = 0; i < items.Count; i += 1)
                {
                    CarriableItem item = items[i];
                    if (item == null)
                    {
                        continue;
                    }

                    CarriableItem selectedItem = item;
                    Button itemButton = new Button(() => TrySelect(selectedItem))
                    {
                        text = selectedItem.StorageDisplayName
                    };
                    StyleActionButton(itemButton);
                    itemButton.style.marginBottom = 8f;
                    itemList.Add(itemButton);
                    visibleItemCount += 1;
                }
            }

            emptyLabel.style.display = visibleItemCount == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void TrySelect(CarriableItem item)
        {
            if (item == null || selectHandler == null)
            {
                return;
            }

            if (selectHandler(item))
            {
                Close(invokeDismiss: false);
            }
        }

        void Close(bool invokeDismiss)
        {
            if (overlay != null)
            {
                overlay.style.display = DisplayStyle.None;
            }

            ReleaseCursorOverride();

            IsOpen = false;

            Action dismiss = dismissHandler;
            selectHandler = null;
            dismissHandler = null;

            if (invokeDismiss)
            {
                dismiss?.Invoke();
            }
        }

        void HideImmediate()
        {
            if (overlay != null)
            {
                overlay.style.display = DisplayStyle.None;
            }

            ReleaseCursorOverride();

            IsOpen = false;
            selectHandler = null;
            dismissHandler = null;
        }

        void ResolvePanelSettings()
        {
            if (uiDocument == null)
            {
                return;
            }

            if (uiDocument.panelSettings == null)
            {
                uiDocument.panelSettings = ResolvePanelSettingsAsset();
            }
        }

        PanelSettings ResolvePanelSettingsAsset()
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

            if (runtimePanelSettings == null)
            {
                runtimePanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                runtimePanelSettings.name = "DrawerStorageDialogPanelSettings";
                runtimePanelSettings.hideFlags = HideFlags.DontSave;
            }

            return runtimePanelSettings;
        }

        static void StyleActionButton(Button button)
        {
            button.style.height = 32f;
            button.style.color = new Color(0.95f, 0.9f, 0.78f, 1f);
            button.style.backgroundColor = new Color(0.27f, 0.2f, 0.12f, 0.95f);
            button.style.borderTopLeftRadius = 4f;
            button.style.borderTopRightRadius = 4f;
            button.style.borderBottomLeftRadius = 4f;
            button.style.borderBottomRightRadius = 4f;
            button.style.borderLeftWidth = 1f;
            button.style.borderRightWidth = 1f;
            button.style.borderTopWidth = 1f;
            button.style.borderBottomWidth = 1f;
            button.style.borderLeftColor = new Color(0.72f, 0.62f, 0.38f, 0.45f);
            button.style.borderRightColor = new Color(0.72f, 0.62f, 0.38f, 0.45f);
            button.style.borderTopColor = new Color(0.72f, 0.62f, 0.38f, 0.45f);
            button.style.borderBottomColor = new Color(0.72f, 0.62f, 0.38f, 0.45f);
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        void OnDestroy()
        {
            ReleaseCursorOverride();

            if (runtimePanelSettings != null)
            {
                Destroy(runtimePanelSettings);
                runtimePanelSettings = null;
            }
        }

        void ReleaseCursorOverride()
        {
            if (!cursorOverrideActive)
            {
                return;
            }

            GameCursorController.PopModalUi();
            cursorOverrideActive = false;
        }
    }
}