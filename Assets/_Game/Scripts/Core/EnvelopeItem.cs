using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Whisperer
{
    public class EnvelopeItem : LetterItem
    {
        [Header("Envelope")]
        [SerializeField] GameObject openedLetterPrefab;

        [Header("Interaction")]
        [SerializeField] string deskOpenActionLabel = "Open";

        public bool IsPlacedOnDesk { get; private set; }
        public bool IsEnvelopeOpened { get; private set; }

        public bool CanOpenFromDesk => IsPlacedOnDesk && !IsEnvelopeOpened;

        public override bool CanPickUpAtDesk()
        {
            return IsPlacedOnDesk && !IsEnvelopeOpened && !IsCarried;
        }

        public override string GetInteractionPrompt(bool isDeskMode)
        {
            if (isDeskMode && CanOpenFromDesk)
            {
                return deskOpenActionLabel;
            }

            return base.GetInteractionPrompt(isDeskMode);
        }

        public override bool PickUpFromDesk(Transform carryAnchor)
        {
            if (!CanPickUpAtDesk() || carryAnchor == null)
            {
                return false;
            }

            PickUp(carryAnchor);
            IsPlacedOnDesk = false;
            return true;
        }

        public override bool OpenAtDesk(Transform deskAnchor)
        {
            if (!IsCarried || deskAnchor == null)
            {
                return false;
            }

            // First desk interaction while carrying places the envelope unopened.
            PlaceAtDesk(deskAnchor, markOpenedAtDesk: false);
            IsPlacedOnDesk = true;
            return true;
        }

        public bool OpenAtDesk(Vector3 worldPosition, Quaternion worldRotation, Transform deskParent = null)
        {
            if (!IsCarried)
            {
                return false;
            }

            PlaceAtDesk(worldPosition, worldRotation, deskParent, markOpenedAtDesk: false);
            IsPlacedOnDesk = true;
            return true;
        }

        public bool OpenFromDesk(Transform deskAnchor)
        {
            if (!CanOpenFromDesk || deskAnchor == null)
            {
                return false;
            }

            if (openedLetterPrefab == null)
            {
                Debug.LogWarning("EnvelopeItem: Opened letter prefab is not assigned.", this);
                return false;
            }

            GameObject openedLetter = Instantiate(openedLetterPrefab, deskAnchor.position, deskAnchor.rotation, deskAnchor);
            openedLetter.name = openedLetterPrefab.name;

            LetterItem letterItem = openedLetter.GetComponentInChildren<LetterItem>();
            if (letterItem == null)
            {
                Debug.LogWarning("EnvelopeItem: Spawned letter prefab is missing LetterItem.", this);
                Destroy(openedLetter);
                return false;
            }

            IsEnvelopeOpened = true;
            letterItem.PlaceAtDesk(deskAnchor);
            DestroyEnvelopeRoot();
            return true;
        }

        public bool OpenFromDesk(Vector3 worldPosition, Quaternion worldRotation, Transform deskParent = null)
        {
            if (!CanOpenFromDesk)
            {
                return false;
            }

            if (openedLetterPrefab == null)
            {
                Debug.LogWarning("EnvelopeItem: Opened letter prefab is not assigned.", this);
                return false;
            }

            GameObject openedLetter = Instantiate(openedLetterPrefab, worldPosition, worldRotation, deskParent);
            openedLetter.name = openedLetterPrefab.name;

            LetterItem letterItem = openedLetter.GetComponentInChildren<LetterItem>();
            if (letterItem == null)
            {
                Debug.LogWarning("EnvelopeItem: Spawned letter prefab is missing LetterItem.", this);
                Destroy(openedLetter);
                return false;
            }

            IsEnvelopeOpened = true;
            letterItem.PlaceAtDesk(worldPosition, worldRotation, deskParent);
            DestroyEnvelopeRoot();
            return true;
        }

        void DestroyEnvelopeRoot()
        {
            GameObject rootObject = ItemRoot.gameObject;
            if (Application.isPlaying)
            {
                Destroy(rootObject);
                return;
            }

#if UNITY_EDITOR
            DestroyImmediate(rootObject);
#else
            Destroy(rootObject);
#endif
        }
    }
}