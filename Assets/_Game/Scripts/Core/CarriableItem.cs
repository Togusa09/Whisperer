using UnityEngine;
using UnityEngine.Serialization;

namespace Whisperer
{
    [RequireComponent(typeof(Collider))]
    public abstract class CarriableItem : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] protected Transform itemRoot;
        [FormerlySerializedAs("letterRigidbody")]
        [FormerlySerializedAs("propRigidbody")]
        [SerializeField] Rigidbody itemRigidbody;
        [FormerlySerializedAs("letterColliders")]
        [FormerlySerializedAs("propColliders")]
        [SerializeField] Collider[] itemColliders;
        [SerializeField] protected Vector3 carriedVisualOffset = new(0f, -0.08f, 0f);
        [SerializeField] float dropForwardOffset = 0.65f;
        [SerializeField] float dropVerticalOffset = -0.15f;
        [SerializeField] float dropVelocity = 1.5f;

        [Header("Interaction")]
        [SerializeField] string exploreActionLabel = "Pick Up";
        [SerializeField] string deskActionLabel = "Pick Up";

        public bool IsCarried { get; protected set; }
        protected Transform ItemRoot => itemRoot != null ? itemRoot : transform;

        protected virtual void Awake()
        {
            EnsureInitialized();
        }

        protected void EnsureInitialized()
        {
            if (itemRoot == null)
            {
                itemRoot = transform.parent != null ? transform.parent : transform;
            }

            if (itemRigidbody == null)
            {
                itemRigidbody = GetComponentInChildren<Rigidbody>();
            }

            if (itemColliders == null || itemColliders.Length == 0)
            {
                itemColliders = GetComponentsInChildren<Collider>();
            }
        }

        public virtual string GetInteractionPrompt(bool isDeskMode)
        {
            return isDeskMode ? deskActionLabel : exploreActionLabel;
        }

        public virtual void PickUp(Transform carryAnchor)
        {
            EnsureInitialized();

            if (carryAnchor == null)
            {
                return;
            }

            IsCarried = true;

            itemRoot.SetParent(carryAnchor, worldPositionStays: false);
            itemRoot.localRotation = Quaternion.identity;
            itemRoot.localPosition = CalculateCarryLocalPosition();

            SetPhysicsForCarriedState();
        }

        public virtual void Drop(Vector3 forwardDirection)
        {
            EnsureInitialized();

            if (!IsCarried)
            {
                return;
            }

            IsCarried = false;
            itemRoot.SetParent(null, worldPositionStays: true);
            if (forwardDirection.sqrMagnitude > 0.0001f)
            {
                itemRoot.position += forwardDirection.normalized * dropForwardOffset + Vector3.up * dropVerticalOffset;
            }

            SetPhysicsForWorldState();

            if (itemRigidbody != null)
            {
                itemRigidbody.linearVelocity = forwardDirection.normalized * dropVelocity;
            }
        }

        protected void PlaceAtDeskCore(Transform deskAnchor)
        {
            if (deskAnchor == null)
            {
                return;
            }

            EnsureInitialized();

            IsCarried = false;
            itemRoot.SetParent(deskAnchor, worldPositionStays: false);
            itemRoot.localPosition = Vector3.zero;
            itemRoot.localRotation = Quaternion.identity;

            SetPhysicsForCarriedState();
        }

        protected void PlaceAtDeskCore(Vector3 worldPosition, Quaternion worldRotation, Transform deskParent)
        {
            EnsureInitialized();

            IsCarried = false;
            itemRoot.SetParent(deskParent, worldPositionStays: true);
            itemRoot.position = worldPosition;
            itemRoot.rotation = worldRotation;

            SetPhysicsForCarriedState();
        }

        protected void SetPhysicsForCarriedState()
        {
            if (itemRigidbody != null)
            {
                itemRigidbody.isKinematic = true;
                itemRigidbody.useGravity = false;
                itemRigidbody.linearVelocity = Vector3.zero;
                itemRigidbody.angularVelocity = Vector3.zero;
            }

            SetColliderTriggers(true);
        }

        protected void SetPhysicsForWorldState()
        {
            if (itemRigidbody != null)
            {
                itemRigidbody.isKinematic = false;
                itemRigidbody.useGravity = true;
            }

            SetColliderTriggers(false);
        }

        void SetColliderTriggers(bool value)
        {
            if (itemColliders == null)
            {
                return;
            }

            for (int i = 0; i < itemColliders.Length; i += 1)
            {
                if (itemColliders[i] != null)
                {
                    itemColliders[i].isTrigger = value;
                }
            }
        }

        protected Vector3 CalculateCarryLocalPosition()
        {
            Renderer[] renderers = ItemRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers == null || renderers.Length == 0)
            {
                return carriedVisualOffset;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i += 1)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 localCenter = ItemRoot.InverseTransformPoint(bounds.center);
            return carriedVisualOffset - localCenter;
        }
    }
}