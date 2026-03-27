using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whisperer
{
    public class DeskDrawerInteractable : StudyInteractable
    {
        static readonly Vector3 DefaultDrawerColliderSize = new(0.52f, 0.12f, 0.08f);

        [SerializeField] string drawerLabel = "Desk Drawer";
        [SerializeField] Transform storageRoot;
        [SerializeField] bool allowAnyCarriableItem = true;

        readonly List<CarriableItem> storedItems = new();
        DrawerStorageDialogController dialogController;

        public override string InteractionPrompt => string.IsNullOrWhiteSpace(drawerLabel) ? "Open Drawer" : $"Open {drawerLabel}";
        public int StoredItemCount => storedItems.Count;
        public IReadOnlyList<CarriableItem> StoredItems => storedItems;

        void Awake()
        {
            ApplyDefaults();
        }

        public static void EnsureSceneDrawers()
        {
            Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < sceneTransforms.Length; i += 1)
            {
                Transform candidate = sceneTransforms[i];
                if (!IsMarkedDrawer(candidate))
                {
                    continue;
                }

                EnsureDrawerInteraction(candidate.gameObject);
            }
        }

        public override string GetInteractionPrompt(PlayerInteractionController controller)
        {
            if (controller == null || !controller.IsDeskMode)
            {
                return string.Empty;
            }

            ApplyDefaults();

            if (controller.HasCarriedItem)
            {
                return CanStoreItem(controller.CarriedItem)
                    ? $"Place {controller.CarriedItem.StorageDisplayName} in {drawerLabel}"
                    : string.Empty;
            }

            return InteractionPrompt;
        }

        public override bool CanInteractWhileCarrying(PlayerInteractionController controller)
        {
            return controller != null && controller.IsDeskMode;
        }

        public override bool TryInteract(PlayerInteractionController controller)
        {
            if (controller == null || !controller.IsDeskMode)
            {
                return false;
            }

            ApplyDefaults();
            PruneMissingItems();

            if (controller.HasCarriedItem)
            {
                return controller.TryStoreCarriedItemInDrawer(this);
            }

            dialogController ??= DrawerStorageDialogController.GetOrCreate();
            controller.SetDrawerDialogOpen(true);
            dialogController.Show(
                drawerLabel,
                storedItems,
                item =>
                {
                    bool success = controller.TryTakeItemFromDrawer(this, item);
                    if (success)
                    {
                        controller.SetDrawerDialogOpen(false);
                    }

                    return success;
                },
                () => controller.SetDrawerDialogOpen(false));
            return true;
        }

        public bool CanStoreItem(CarriableItem item)
        {
            if (item == null || !item.CanStoreInDrawer || storedItems.Contains(item))
            {
                return false;
            }

            return allowAnyCarriableItem || item is LetterItem || item is EnvelopeItem;
        }

        public bool StoreItem(CarriableItem item)
        {
            if (!CanStoreItem(item))
            {
                return false;
            }

            ApplyDefaults();
            storedItems.Add(item);
            item.StoreInContainer(storageRoot);
            return true;
        }

        public bool TakeItem(CarriableItem item, Transform carryAnchor)
        {
            if (item == null || carryAnchor == null)
            {
                return false;
            }

            PruneMissingItems();
            if (!storedItems.Contains(item))
            {
                return false;
            }

            if (!item.TryRetrieveFromContainer(carryAnchor))
            {
                return false;
            }

            storedItems.Remove(item);
            return true;
        }

        static bool IsMarkedDrawer(Transform candidate)
        {
            return candidate != null
                && candidate.parent != null
                && candidate.parent.name == "PidgeonHoles"
                && candidate.name.EndsWith("Drawer", StringComparison.OrdinalIgnoreCase);
        }

        static void EnsureDrawerInteraction(GameObject drawerObject)
        {
            if (drawerObject == null)
            {
                return;
            }

            Collider drawerCollider = drawerObject.GetComponent<Collider>();
            if (drawerCollider == null)
            {
                BoxCollider boxCollider = drawerObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.center = Vector3.zero;
                boxCollider.size = DefaultDrawerColliderSize;
            }

            DeskDrawerInteractable interactable = drawerObject.GetComponent<DeskDrawerInteractable>();
            if (interactable == null)
            {
                interactable = drawerObject.AddComponent<DeskDrawerInteractable>();
            }

            interactable.ApplyDefaults();
        }

        void ApplyDefaults()
        {
            if (string.IsNullOrWhiteSpace(drawerLabel))
            {
                drawerLabel = HumanizeName(gameObject.name);
            }

            if (storageRoot == null)
            {
                storageRoot = transform;
            }
        }

        void PruneMissingItems()
        {
            storedItems.RemoveAll(item => item == null);
        }

        static string HumanizeName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return "Drawer";
            }

            string cleaned = rawName.Replace('_', ' ').Trim();
            System.Text.StringBuilder builder = new System.Text.StringBuilder(cleaned.Length + 8);
            for (int i = 0; i < cleaned.Length; i += 1)
            {
                char current = cleaned[i];
                if (i > 0 && char.IsUpper(current) && cleaned[i - 1] != ' ' && !char.IsUpper(cleaned[i - 1]))
                {
                    builder.Append(' ');
                }

                builder.Append(current);
            }

            return builder.ToString().Trim();
        }
    }
}