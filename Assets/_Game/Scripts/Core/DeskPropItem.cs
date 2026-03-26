using UnityEngine;

namespace Whisperer
{
    [RequireComponent(typeof(Collider))]
    public class DeskPropItem : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] Transform itemRoot;
        [SerializeField] Rigidbody propRigidbody;
        [SerializeField] Collider[] propColliders;
        [SerializeField] Vector3 carriedVisualOffset = new(0f, -0.06f, 0f);
        [SerializeField] float dropForwardOffset = 0.55f;
        [SerializeField] float dropVerticalOffset = -0.1f;
        [SerializeField] float dropVelocity = 1.2f;

        public bool IsCarried { get; private set; }
        public bool IsPlacedAtDesk { get; private set; }

        void Awake()
        {
            EnsureInitialized();
        }

        void EnsureInitialized()
        {
            if (itemRoot == null)
            {
                itemRoot = transform.parent != null ? transform.parent : transform;
            }

            if (propRigidbody == null)
            {
                propRigidbody = GetComponentInChildren<Rigidbody>();
            }

            if (propColliders == null || propColliders.Length == 0)
            {
                propColliders = GetComponentsInChildren<Collider>();
            }
        }

        public void PickUp(Transform carryAnchor)
        {
            EnsureInitialized();
            if (carryAnchor == null)
            {
                return;
            }

            IsCarried = true;
            IsPlacedAtDesk = false;

            itemRoot.SetParent(carryAnchor, worldPositionStays: false);
            itemRoot.localRotation = Quaternion.identity;
            itemRoot.localPosition = CalculateCarryLocalPosition();

            SetPhysicsForCarriedState();
        }

        public void Drop(Vector3 forwardDirection)
        {
            EnsureInitialized();
            if (!IsCarried)
            {
                return;
            }

            IsCarried = false;
            IsPlacedAtDesk = false;

            itemRoot.SetParent(null, worldPositionStays: true);
            if (forwardDirection.sqrMagnitude > 0.0001f)
            {
                itemRoot.position += forwardDirection.normalized * dropForwardOffset + Vector3.up * dropVerticalOffset;
            }

            SetPhysicsForWorldState();

            if (propRigidbody != null)
            {
                propRigidbody.linearVelocity = forwardDirection.normalized * dropVelocity;
            }
        }

        public void PlaceAtDesk(Transform deskAnchor)
        {
            if (deskAnchor == null)
            {
                return;
            }

            PlaceAtDesk(deskAnchor.position, deskAnchor.rotation, deskAnchor);
        }

        public void PlaceAtDesk(Vector3 worldPosition, Quaternion worldRotation, Transform deskParent = null)
        {
            EnsureInitialized();

            IsCarried = false;
            IsPlacedAtDesk = true;

            itemRoot.SetParent(deskParent, worldPositionStays: true);
            itemRoot.position = worldPosition;
            itemRoot.rotation = worldRotation;

            SetPhysicsForCarriedState();
        }

        void SetPhysicsForCarriedState()
        {
            if (propRigidbody != null)
            {
                propRigidbody.isKinematic = true;
                propRigidbody.useGravity = false;
                propRigidbody.linearVelocity = Vector3.zero;
                propRigidbody.angularVelocity = Vector3.zero;
            }

            SetColliderTriggers(true);
        }

        void SetPhysicsForWorldState()
        {
            if (propRigidbody != null)
            {
                propRigidbody.isKinematic = false;
                propRigidbody.useGravity = true;
            }

            SetColliderTriggers(false);
        }

        void SetColliderTriggers(bool value)
        {
            if (propColliders == null)
            {
                return;
            }

            for (int i = 0; i < propColliders.Length; i += 1)
            {
                if (propColliders[i] != null)
                {
                    propColliders[i].isTrigger = value;
                }
            }
        }

        Vector3 CalculateCarryLocalPosition()
        {
            Renderer[] renderers = itemRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers == null || renderers.Length == 0)
            {
                return carriedVisualOffset;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i += 1)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 localCenter = itemRoot.InverseTransformPoint(bounds.center);
            return carriedVisualOffset - localCenter;
        }
    }
}
