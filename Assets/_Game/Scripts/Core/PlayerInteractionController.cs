using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System;

namespace Whisperer
{
    public class PlayerInteractionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Camera interactionCamera;
        [SerializeField] Transform carryAnchor;
        [SerializeField] Transform deskCarryAnchor;
        [SerializeField] Transform deskLetterAnchor;
        [SerializeField] Transform deskPlacementParent;
        [SerializeField] PlayerModeSwitcher modeSwitcher;

        [Header("Interaction")]
        [SerializeField] float interactDistance = 2.5f;
        [SerializeField] LayerMask interactionMask = ~0;
        [SerializeField] LayerMask deskSurfaceMask = ~0;
        [SerializeField] float deskSurfaceOffset = 0.005f;
        [SerializeField] string deskEntryTag = "DeskEntry";

        [Header("Desk Feedback")]
        [SerializeField] InteractionHudController interactionHud;

        [Header("Desk Mode Carry Visibility")]
        [SerializeField] float deskModeCarryDistance = 0.4f;
        [SerializeField] float deskModeCarryHeight = 0.08f;

        CarriableItem carriedItem;
        bool shouldPlaceOnDeskNextFrame = false;
    bool drawerDialogOpen;
        DeskModeInteractable lastUsedDeskInteractable;

        public bool IsDeskMode => modeSwitcher != null && modeSwitcher.CurrentMode == PlayerMode.Desk;
    public bool HasCarriedItem => carriedItem != null;
    public CarriableItem CarriedItem => carriedItem;

        Camera ActiveInteractionCamera
        {
            get
            {
                if (modeSwitcher != null && modeSwitcher.ActiveCamera != null)
                {
                    return modeSwitcher.ActiveCamera;
                }

                return interactionCamera != null ? interactionCamera : Camera.main;
            }
        }

        void Awake()
        {
            if (modeSwitcher == null)
            {
                modeSwitcher = FindAnyObjectByType<PlayerModeSwitcher>();
            }

            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }

            if (carryAnchor == null && interactionCamera != null)
            {
                carryAnchor = interactionCamera.transform;
            }

            if (deskPlacementParent == null && deskLetterAnchor != null)
            {
                deskPlacementParent = deskLetterAnchor.parent;
            }

            EnsureInteractionHud();
            DeskDrawerInteractable.EnsureSceneDrawers();
        }

        void OnEnable()
        {
            EnsureInteractionHud();
            DeskDrawerInteractable.EnsureSceneDrawers();
        }

        void OnDisable()
        {
        }

        void EnsureInteractionHud()
        {
            if (interactionHud != null)
            {
                interactionHud.SetVisible(true);
                return;
            }

            interactionHud = FindAnyObjectByType<InteractionHudController>();
            if (interactionHud != null)
            {
                interactionHud.SetVisible(true);
                return;
            }

            GameObject hudObject = new GameObject("InteractionHud");
            hudObject.transform.SetParent(transform, worldPositionStays: false);

            UIDocument document = hudObject.AddComponent<UIDocument>();
            interactionHud = hudObject.AddComponent<InteractionHudController>();
            document.sortingOrder = short.MaxValue;
            interactionHud.SetVisible(true);
        }

        void Update()
        {
            // Handle placing carried item on desk when entering desk mode
            if (shouldPlaceOnDeskNextFrame && IsDeskMode && carriedItem != null)
            {
                shouldPlaceOnDeskNextFrame = false;
                TryPlaceCarriedItemOnDesk();
                return;
            }

            if (drawerDialogOpen)
            {
                interactionHud?.SetState(false, "");
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                HandleInteract();
            }

            if (keyboard.escapeKey.wasPressedThisFrame && IsDeskMode)
            {
                modeSwitcher.EnterExploreMode();
            }

            RefreshHoverState();
        }

        void HandleInteract()
        {
            if (carriedItem is DeskPropItem carriedDeskProp)
            {
                HandleCarriedDeskPropInteract(carriedDeskProp);
                return;
            }

            if (carriedItem != null)
            {
                if (TryGetInteractableHit(out StudyInteractable carriedInteractable))
                {
                    if (IsDeskMode)
                    {
                        if (carriedInteractable.CanInteractWhileCarrying(this) && carriedInteractable.TryInteract(this))
                        {
                            return;
                        }
                    }
                    else if (carriedInteractable.TryInteract(this))
                    {
                        shouldPlaceOnDeskNextFrame = true;
                        return;
                    }
                }

                if (!IsDeskMode && TryEnterDeskModeByProximity())
                {
                    shouldPlaceOnDeskNextFrame = true;
                    return;
                }

                if (IsDeskMode)
                {
                    if (carriedItem is EnvelopeItem carriedEnvelope && carriedEnvelope.CanOpenFromDesk)
                    {
                        if (TryGetDeskPlacementPose(out Vector3 openPosition, out Quaternion openRotation))
                        {
                            if (carriedEnvelope.OpenFromDesk(openPosition, openRotation, GetDeskPlacementParent()))
                            {
                                carriedItem = null;
                                return;
                            }
                        }

                        if (carriedEnvelope.OpenFromDesk(deskLetterAnchor))
                        {
                            carriedItem = null;
                            return;
                        }
                    }

                    LetterItem carriedLetter = carriedItem as LetterItem;
                    if (carriedLetter == null)
                    {
                        Debug.LogWarning("PlayerInteractionController: Unsupported carried item cannot be placed at desk.", this);
                        return;
                    }

                    if (carriedLetter.CanPickUpAtDesk() || carriedLetter.IsOpenedAtDesk)
                    {
                        if (TryGetDeskPlacementPose(out Vector3 placePosition, out Quaternion placeRotation))
                        {
                            carriedLetter.PlaceAtDesk(placePosition, placeRotation, GetDeskPlacementParent());
                            carriedItem = null;
                            return;
                        }

                        if (deskLetterAnchor != null)
                        {
                            carriedLetter.PlaceAtDesk(deskLetterAnchor);
                            carriedItem = null;
                            return;
                        }

                        Debug.LogWarning("PlayerInteractionController: Desk placement target is missing; cannot place item at desk.", this);
                        carriedItem = null;
                        return;
                    }

                    if (carriedItem is EnvelopeItem carriedEnvelopeToPlace)
                    {
                        if (TryGetDeskPlacementPose(out Vector3 envelopePosition, out Quaternion envelopeRotation))
                        {
                            if (carriedEnvelopeToPlace.OpenAtDesk(envelopePosition, envelopeRotation, GetDeskPlacementParent()))
                            {
                                carriedItem = null;
                                return;
                            }
                        }

                        if (carriedEnvelopeToPlace.OpenAtDesk(deskLetterAnchor))
                        {
                            carriedItem = null;
                            return;
                        }

                        Debug.LogWarning("PlayerInteractionController: Desk letter anchor is missing; cannot place envelope at desk.", this);
                        return;
                    }

                    if (TryGetDeskPlacementPose(out Vector3 letterPosition, out Quaternion letterRotation))
                    {
                        carriedLetter.PlaceAtDesk(letterPosition, letterRotation, GetDeskPlacementParent());
                        carriedItem = null;
                        return;
                    }

                    if (!carriedLetter.OpenAtDesk(deskLetterAnchor))
                    {
                        Debug.LogWarning("PlayerInteractionController: Desk letter anchor is missing; cannot open letter at desk.", this);
                        return;
                    }

                    carriedItem = null;
                    return;
                }

                Camera activeCamera = ActiveInteractionCamera;
                Vector3 dropForward = activeCamera != null ? activeCamera.transform.forward : transform.forward;
                carriedItem.Drop(dropForward);
                carriedItem = null;
                return;
            }

            if (!TryGetInteractionTarget(out CarriableItem item, out StudyInteractable interactable))
            {
                return;
            }

            if (item != null)
            {
                if (IsDeskMode && item is EnvelopeItem envelope && envelope.CanOpenFromDesk)
                {
                    envelope.OpenFromDesk(deskLetterAnchor);
                    return;
                }

                if (!IsDeskMode && item is LetterItem openedLetter && openedLetter.IsOpenedAtDesk)
                {
                    return;
                }

                if (!TryPickUpItem(item) && IsDeskMode && item is LetterItem placedLetter && placedLetter.IsOpenedAtDesk)
                {
                    if (TryGetDeskPlacementPose(out Vector3 itemPosition, out Quaternion itemRotation))
                    {
                        placedLetter.PlaceAtDesk(itemPosition, itemRotation, GetDeskPlacementParent());
                    }
                    else if (deskLetterAnchor != null)
                    {
                        placedLetter.PlaceAtDesk(deskLetterAnchor);
                    }
                }
                return;
            }

            interactable?.TryInteract(this);
        }

        void HandleCarriedDeskPropInteract(DeskPropItem carriedDeskProp)
        {
            if (carriedDeskProp == null)
            {
                return;
            }

            if (IsDeskMode && TryGetInteractableHit(out StudyInteractable carriedInteractable))
            {
                if (carriedInteractable.CanInteractWhileCarrying(this) && carriedInteractable.TryInteract(this))
                {
                    return;
                }
            }

            if (IsDeskMode)
            {
                if (TryGetDeskPlacementPose(out Vector3 worldPosition, out Quaternion worldRotation))
                {
                    carriedDeskProp.PlaceAtDesk(worldPosition, worldRotation, GetDeskPlacementParent());
                    carriedItem = null;
                    return;
                }

                if (deskLetterAnchor != null)
                {
                    carriedDeskProp.PlaceAtDesk(deskLetterAnchor);
                    carriedItem = null;
                    return;
                }

                Debug.LogWarning("PlayerInteractionController: Desk placement target is missing; cannot place desk prop.", this);
                return;
            }

            Camera activeCamera = ActiveInteractionCamera;
            Vector3 dropForward = activeCamera != null ? activeCamera.transform.forward : transform.forward;
            carriedDeskProp.Drop(dropForward);
            carriedItem = null;
        }

        bool TryGetInteractionTarget(out CarriableItem item, out StudyInteractable interactable)
        {
            item = null;
            interactable = null;

            Camera activeCamera = ActiveInteractionCamera;
            if (activeCamera == null)
            {
                return false;
            }

            Ray ray = new Ray(activeCamera.transform.position, activeCamera.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactionMask, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i += 1)
            {
                // Skip desk entry colliders when already in desk mode.
                if (IsDeskMode && !string.IsNullOrEmpty(deskEntryTag) && hits[i].collider.CompareTag(deskEntryTag))
                {
                    continue;
                }

                CarriableItem hitItem = hits[i].collider.GetComponentInParent<CarriableItem>();
                if (carriedItem != null && hitItem == carriedItem)
                {
                    continue;
                }

                if (hitItem != null)
                {
                    item = hitItem;
                    return true;
                }

                StudyInteractable hitInteractable = hits[i].collider.GetComponentInParent<StudyInteractable>();
                if (hitInteractable != null)
                {
                    interactable = hitInteractable;
                    return true;
                }
            }

            return false;
        }

        bool TryPickUpItem(CarriableItem item)
        {
            if (item == null || HasCarriedItem)
            {
                return false;
            }

            Transform targetCarryAnchor = GetActiveCarryAnchor();
            if (targetCarryAnchor == null)
            {
                Debug.LogWarning("PlayerInteractionController: Carry anchor is missing.", this);
                return false;
            }

            if (item is EnvelopeItem envelope)
            {
                return TryPickUpEnvelope(envelope, targetCarryAnchor);
            }

            if (item is LetterItem letter)
            {
                return TryPickUpLetter(letter, targetCarryAnchor);
            }

            if (item is DeskPropItem deskProp)
            {
                return TryPickUpDeskProp(deskProp, targetCarryAnchor);
            }

            return false;
        }

        bool TryPickUpLetter(LetterItem item, Transform targetCarryAnchor)
        {
            if (IsDeskMode)
            {
                if (!item.PickUpFromDesk(targetCarryAnchor))
                {
                    return false;
                }
            }
            else
            {
                item.PickUp(targetCarryAnchor);
            }

            carriedItem = item;

            if (IsDeskMode)
            {
                AdjustCarriedItemForDeskVisibility();
            }

            return true;
        }

        bool TryPickUpEnvelope(EnvelopeItem envelope, Transform targetCarryAnchor)
        {
            if (IsDeskMode)
            {
                if (!envelope.PickUpFromDesk(targetCarryAnchor))
                {
                    return false;
                }
            }
            else
            {
                envelope.PickUp(targetCarryAnchor);
            }

            carriedItem = envelope;

            if (IsDeskMode)
            {
                AdjustCarriedItemForDeskVisibility();
            }

            return true;
        }

        bool TryPickUpDeskProp(DeskPropItem deskProp, Transform targetCarryAnchor)
        {
            if (deskProp == null || HasCarriedItem)
            {
                return false;
            }

            deskProp.PickUp(targetCarryAnchor);
            carriedItem = deskProp;

            if (IsDeskMode)
            {
                AdjustCarriedItemForDeskVisibility();
            }

            return true;
        }

        Transform GetActiveCarryAnchor()
        {
            Transform targetCarryAnchor = IsDeskMode ? deskCarryAnchor : carryAnchor;
            if (targetCarryAnchor != null)
            {
                return targetCarryAnchor;
            }

            Camera activeCamera = ActiveInteractionCamera;
            return activeCamera != null ? activeCamera.transform : null;
        }

        bool TryGetInteractableHit(out StudyInteractable interactable)
        {
            interactable = null;
            if (!TryGetInteractionTarget(out _, out StudyInteractable targetInteractable))
            {
                return false;
            }

            interactable = targetInteractable;
            return interactable != null;
        }

        bool TryEnterDeskModeByProximity()
        {
            Camera activeCamera = ActiveInteractionCamera;
            if (activeCamera == null || modeSwitcher == null)
            {
                return false;
            }

            DeskModeInteractable[] desks = FindObjectsByType<DeskModeInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < desks.Length; i += 1)
            {
                if (desks[i] == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(activeCamera.transform.position, desks[i].transform.position);
                if (distance <= interactDistance * 1.5f)
                {
                    lastUsedDeskInteractable = desks[i];
                    modeSwitcher.EnterDeskMode();
                    return true;
                }
            }

            return false;
        }

        void TryPlaceCarriedItemOnDesk()
        {
            if (carriedItem == null || !IsDeskMode)
            {
                return;
            }

            if (carriedItem is EnvelopeItem envelope)
            {
                // During desk mode transition, prefer the explicit desk anchor for deterministic placement.
                if (deskLetterAnchor != null && envelope.OpenAtDesk(deskLetterAnchor))
                {
                    carriedItem = null;
                    return;
                }

                if (TryGetDeskPlacementPose(out Vector3 envelopePosition, out Quaternion envelopeRotation))
                {
                    if (envelope.OpenAtDesk(envelopePosition, envelopeRotation, GetDeskPlacementParent()))
                    {
                        carriedItem = null;
                        return;
                    }
                }

                if (envelope.OpenAtDesk(deskLetterAnchor))
                {
                    carriedItem = null;
                    return;
                }

                Debug.LogWarning("PlayerInteractionController: Desk letter anchor is missing; cannot place carried envelope at desk.", this);
                return;
            }

            LetterItem letter = carriedItem as LetterItem;
            if (letter == null)
            {
                Debug.LogWarning("PlayerInteractionController: Unsupported carried item cannot be auto-placed at desk.", this);
                return;
            }

            if (deskLetterAnchor != null && letter.OpenAtDesk(deskLetterAnchor))
            {
                carriedItem = null;
                return;
            }

            if (TryGetDeskPlacementPose(out Vector3 worldPosition, out Quaternion worldRotation))
            {
                letter.PlaceAtDesk(worldPosition, worldRotation, GetDeskPlacementParent());
                carriedItem = null;
                return;
            }

            if (letter.OpenAtDesk(deskLetterAnchor))
            {
                carriedItem = null;
                return;
            }

            Debug.LogWarning("PlayerInteractionController: Desk letter anchor is missing; cannot place carried mail at desk.", this);
        }

        bool TryGetDeskPlacementPose(out Vector3 worldPosition, out Quaternion worldRotation)
        {
            worldPosition = default;
            worldRotation = Quaternion.identity;

            Camera activeCamera = ActiveInteractionCamera;
            if (activeCamera == null)
            {
                return false;
            }

            int mask = deskSurfaceMask.value != 0 ? deskSurfaceMask : interactionMask;
            Ray ray = new Ray(activeCamera.transform.position, activeCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, mask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            worldPosition = hit.point + hit.normal * deskSurfaceOffset;

            Vector3 planarForward = Vector3.ProjectOnPlane(activeCamera.transform.forward, hit.normal);
            if (planarForward.sqrMagnitude < 0.0001f)
            {
                planarForward = Vector3.ProjectOnPlane(activeCamera.transform.up, hit.normal);
            }

            if (planarForward.sqrMagnitude < 0.0001f)
            {
                planarForward = Vector3.forward;
            }

            worldRotation = Quaternion.LookRotation(planarForward.normalized, hit.normal);
            return true;
        }

        Transform GetDeskPlacementParent()
        {
            if (deskPlacementParent != null)
            {
                return deskPlacementParent;
            }

            return deskLetterAnchor != null ? deskLetterAnchor.parent : null;
        }

        void RefreshHoverState()
        {
            if (drawerDialogOpen)
            {
                interactionHud?.SetState(false, "");
                return;
            }

            if (!TryGetInteractionTarget(out CarriableItem item, out StudyInteractable interactable))
            {
                interactionHud?.SetState(false, "");
                return;
            }

            string actionLabel = "";

            if (item != null)
            {
                if (!IsDeskMode && carriedItem != null)
                {
                    interactionHud?.SetState(true, "");
                    return;
                }

                actionLabel = item.GetInteractionPrompt(IsDeskMode);
            }
            else if (interactable != null)
            {
                actionLabel = interactable.GetInteractionPrompt(this);
                if (string.IsNullOrEmpty(actionLabel))
                {
                    interactionHud?.SetState(false, "");
                    return;
                }
            }

            string prompt = string.IsNullOrEmpty(actionLabel) ? "" : $"Press E to {actionLabel}";
            interactionHud?.SetState(true, prompt);
        }

        void AdjustCarriedItemForDeskVisibility()
        {
            if (carriedItem == null)
            {
                return;
            }

            Camera deskCamera = modeSwitcher?.DeskCamera;
            if (deskCamera == null)
            {
                return;
            }

            Transform itemTransform = carriedItem.transform;
            Vector3 desiredWorldPosition = deskCamera.transform.position
                + deskCamera.transform.forward * deskModeCarryDistance
                + deskCamera.transform.up * deskModeCarryHeight;
            
            // Position the item in front of the camera without moving its parent anchor.
            if (itemTransform.parent != null)
            {
                itemTransform.localPosition = itemTransform.parent.InverseTransformPoint(desiredWorldPosition);
            }
            else
            {
                itemTransform.position = desiredWorldPosition;
            }
        }

        public bool TryStoreCarriedItemInDrawer(DeskDrawerInteractable drawer)
        {
            if (drawer == null || carriedItem == null)
            {
                return false;
            }

            if (!drawer.StoreItem(carriedItem))
            {
                return false;
            }

            carriedItem = null;
            return true;
        }

        public bool TryTakeItemFromDrawer(DeskDrawerInteractable drawer, CarriableItem item)
        {
            if (drawer == null || item == null || HasCarriedItem)
            {
                return false;
            }

            Transform targetCarryAnchor = GetActiveCarryAnchor();
            if (targetCarryAnchor == null)
            {
                return false;
            }

            if (!drawer.TakeItem(item, targetCarryAnchor))
            {
                return false;
            }

            carriedItem = item;
            if (IsDeskMode)
            {
                AdjustCarriedItemForDeskVisibility();
            }

            return true;
        }

        public void SetDrawerDialogOpen(bool isOpen)
        {
            drawerDialogOpen = isOpen;
            if (isOpen)
            {
                interactionHud?.SetState(false, "");
            }
        }

    }
}
