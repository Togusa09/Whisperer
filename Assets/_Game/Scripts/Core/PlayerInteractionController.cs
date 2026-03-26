using UnityEngine;
using UnityEngine.InputSystem;
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
        [SerializeField] bool showInteractionReticle = true;
        [SerializeField] Color reticleIdleColor = new(1f, 1f, 1f, 0.3f);
        [SerializeField] Color reticleHoverColor = new(0.95f, 0.83f, 0.35f, 0.95f);
        [SerializeField] float reticleSize = 10f;
        [SerializeField] bool showInteractionPrompt = true;
        [SerializeField] Color promptTextColor = new(1f, 1f, 1f, 0.9f);
        [SerializeField] int promptFontSize = 16;

        [Header("Desk Mode Carry Visibility")]
        [SerializeField] float deskModeCarryDistance = 0.4f;
        [SerializeField] float deskModeCarryHeight = 0.08f;

        LetterItem carriedItem;
        DeskPropItem carriedDeskProp;
        bool shouldPlaceOnDeskNextFrame = false;
        bool isHoveringTarget;
        string lastInteractionPrompt = "";
        DeskModeInteractable lastUsedDeskInteractable;

        public bool IsDeskMode => modeSwitcher != null && modeSwitcher.CurrentMode == PlayerMode.Desk;
        bool HasCarriedItem => carriedItem != null || carriedDeskProp != null;

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
        }

        void OnEnable()
        {
        }

        void OnDisable()
        {
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
            if (carriedDeskProp != null)
            {
                HandleCarriedDeskPropInteract();
                return;
            }

            if (carriedItem != null)
            {
                if (TryGetInteractableHit(out StudyInteractable carriedInteractable))
                {
                    if (!IsDeskMode && carriedInteractable.TryInteract(this))
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

                    if (carriedItem.CanPickUpAtDesk() || carriedItem.IsOpenedAtDesk)
                    {
                        if (TryGetDeskPlacementPose(out Vector3 placePosition, out Quaternion placeRotation))
                        {
                            carriedItem.PlaceAtDesk(placePosition, placeRotation, GetDeskPlacementParent());
                            carriedItem = null;
                            return;
                        }

                        if (deskLetterAnchor != null)
                        {
                            carriedItem.PlaceAtDesk(deskLetterAnchor);
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
                        carriedItem.PlaceAtDesk(letterPosition, letterRotation, GetDeskPlacementParent());
                        carriedItem = null;
                        return;
                    }

                    if (!carriedItem.OpenAtDesk(deskLetterAnchor))
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

            if (!TryGetInteractionTarget(out LetterItem item, out DeskPropItem deskProp, out StudyInteractable interactable))
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

                if (!IsDeskMode && item.IsOpenedAtDesk)
                {
                    return;
                }

                if (!TryPickUpItem(item) && IsDeskMode && item.IsOpenedAtDesk)
                {
                    if (TryGetDeskPlacementPose(out Vector3 itemPosition, out Quaternion itemRotation))
                    {
                        item.PlaceAtDesk(itemPosition, itemRotation, GetDeskPlacementParent());
                    }
                    else if (deskLetterAnchor != null)
                    {
                        item.PlaceAtDesk(deskLetterAnchor);
                    }
                }
                return;
            }

            if (deskProp != null)
            {
                TryPickUpDeskProp(deskProp);
                return;
            }

            interactable?.TryInteract(this);
        }

        void HandleCarriedDeskPropInteract()
        {
            if (carriedDeskProp == null)
            {
                return;
            }

            if (IsDeskMode)
            {
                if (TryGetDeskPlacementPose(out Vector3 worldPosition, out Quaternion worldRotation))
                {
                    carriedDeskProp.PlaceAtDesk(worldPosition, worldRotation, GetDeskPlacementParent());
                    carriedDeskProp = null;
                    return;
                }

                if (deskLetterAnchor != null)
                {
                    carriedDeskProp.PlaceAtDesk(deskLetterAnchor);
                    carriedDeskProp = null;
                    return;
                }

                Debug.LogWarning("PlayerInteractionController: Desk placement target is missing; cannot place desk prop.", this);
                return;
            }

            Camera activeCamera = ActiveInteractionCamera;
            Vector3 dropForward = activeCamera != null ? activeCamera.transform.forward : transform.forward;
            carriedDeskProp.Drop(dropForward);
            carriedDeskProp = null;
        }

        bool TryGetInteractionTarget(out LetterItem item, out DeskPropItem deskProp, out StudyInteractable interactable)
        {
            item = null;
            deskProp = null;
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

                LetterItem hitItem = hits[i].collider.GetComponentInParent<LetterItem>();
                if (carriedItem != null && hitItem == carriedItem)
                {
                    continue;
                }

                if (hitItem != null)
                {
                    item = hitItem;
                    return true;
                }

                DeskPropItem hitDeskProp = hits[i].collider.GetComponentInParent<DeskPropItem>();
                if (carriedDeskProp != null && hitDeskProp == carriedDeskProp)
                {
                    continue;
                }

                if (hitDeskProp != null)
                {
                    deskProp = hitDeskProp;
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

        bool TryPickUpItem(LetterItem item)
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

        bool TryPickUpDeskProp(DeskPropItem deskProp)
        {
            if (deskProp == null || HasCarriedItem)
            {
                return false;
            }

            Transform targetCarryAnchor = GetActiveCarryAnchor();
            if (targetCarryAnchor == null)
            {
                Debug.LogWarning("PlayerInteractionController: Carry anchor is missing.", this);
                return false;
            }

            deskProp.PickUp(targetCarryAnchor);
            carriedDeskProp = deskProp;

            if (IsDeskMode)
            {
                AdjustCarriedDeskPropForDeskVisibility();
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
            if (!TryGetInteractionTarget(out _, out _, out StudyInteractable targetInteractable))
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

            if (deskLetterAnchor != null && carriedItem.OpenAtDesk(deskLetterAnchor))
            {
                carriedItem = null;
                return;
            }

            if (TryGetDeskPlacementPose(out Vector3 worldPosition, out Quaternion worldRotation))
            {
                carriedItem.PlaceAtDesk(worldPosition, worldRotation, GetDeskPlacementParent());
                carriedItem = null;
                return;
            }

            if (carriedItem.OpenAtDesk(deskLetterAnchor))
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
            isHoveringTarget = false;
            lastInteractionPrompt = "";

            if (!TryGetInteractionTarget(out LetterItem item, out DeskPropItem deskProp, out StudyInteractable interactable))
            {
                return;
            }

            isHoveringTarget = true;

            if (IsDeskMode)
            {
                if (item != null)
                {
                    if (item is EnvelopeItem envelope && envelope.CanOpenFromDesk)
                    {
                        lastInteractionPrompt = "Press E to Open";
                    }
                    else if (item.CanPickUpAtDesk() || item.IsOpenedAtDesk)
                    {
                        lastInteractionPrompt = "Press E to Move";
                    }
                    else
                    {
                        lastInteractionPrompt = "Press E to Place";
                    }
                }
                else if (deskProp != null)
                {
                    lastInteractionPrompt = "Press E to Move";
                }
                else if (interactable != null)
                {
                    lastInteractionPrompt = "Press E to Interact";
                }
            }
            else
            {
                if (item != null)
                {
                    lastInteractionPrompt = carriedItem != null ? "" : "Press E to Pick Up";
                }
                else if (deskProp != null)
                {
                    lastInteractionPrompt = carriedDeskProp != null ? "" : "Press E to Pick Up";
                }
                else if (interactable != null)
                {
                    lastInteractionPrompt = "Press E to Interact";
                }
            }
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

        void AdjustCarriedDeskPropForDeskVisibility()
        {
            if (carriedDeskProp == null)
            {
                return;
            }

            Camera deskCamera = modeSwitcher?.DeskCamera;
            if (deskCamera == null)
            {
                return;
            }

            Transform propTransform = carriedDeskProp.transform;
            Vector3 desiredWorldPosition = deskCamera.transform.position
                + deskCamera.transform.forward * deskModeCarryDistance
                + deskCamera.transform.up * deskModeCarryHeight;
            
            // Position the prop in front of the camera without moving its parent anchor.
            if (propTransform.parent != null)
            {
                propTransform.localPosition = propTransform.parent.InverseTransformPoint(desiredWorldPosition);
            }
            else
            {
                propTransform.position = desiredWorldPosition;
            }
        }

        void OnGUI()
        {
            if (!showInteractionReticle)
            {
                return;
            }

            float size = Mathf.Max(2f, reticleSize);
            float half = size * 0.5f;
            Rect reticleRect = new Rect((Screen.width * 0.5f) - half, (Screen.height * 0.5f) - half, size, size);
            
            Color previousColor = GUI.color;
            GUI.color = isHoveringTarget ? reticleHoverColor : reticleIdleColor;
            GUI.DrawTexture(reticleRect, Texture2D.whiteTexture);
            GUI.color = previousColor;

            if (showInteractionPrompt && !string.IsNullOrEmpty(lastInteractionPrompt))
            {
                GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = promptFontSize,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                promptStyle.normal.textColor = promptTextColor;

                Rect promptRect = new Rect(
                    (Screen.width * 0.5f) - 150f,
                    (Screen.height * 0.5f) + 30f,
                    300f,
                    50f
                );
                GUI.Label(promptRect, lastInteractionPrompt, promptStyle);
            }
        }
    }
}
