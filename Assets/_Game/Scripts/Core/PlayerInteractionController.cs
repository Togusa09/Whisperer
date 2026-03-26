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
        [SerializeField] PlayerModeSwitcher modeSwitcher;

        [Header("Interaction")]
        [SerializeField] float interactDistance = 2.5f;
        [SerializeField] LayerMask interactionMask = ~0;

        LetterItem carriedItem;
        bool shouldPlaceOnDeskNextFrame = false;

        public bool IsDeskMode => modeSwitcher != null && modeSwitcher.CurrentMode == PlayerMode.Desk;

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
        }

        void HandleInteract()
        {
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
                        carriedEnvelope.OpenFromDesk(deskLetterAnchor);
                        carriedItem = null;
                        return;
                    }

                    if (carriedItem.CanPickUpAtDesk() || carriedItem.IsOpenedAtDesk)
                    {
                        carriedItem.PlaceAtDesk(deskLetterAnchor);
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

            if (!TryGetInteractionTarget(out LetterItem item, out StudyInteractable interactable))
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
                    item.PlaceAtDesk(deskLetterAnchor);
                }
                return;
            }

            interactable?.TryInteract(this);
        }

        bool TryGetInteractionTarget(out LetterItem item, out StudyInteractable interactable)
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
            if (item == null || carriedItem != null)
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

            if (carriedItem.OpenAtDesk(deskLetterAnchor))
            {
                carriedItem = null;
                return;
            }

            Debug.LogWarning("PlayerInteractionController: Desk letter anchor is missing; cannot place carried mail at desk.", this);
        }
    }
}
