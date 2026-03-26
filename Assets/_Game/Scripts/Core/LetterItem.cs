using UnityEngine;

namespace Whisperer
{
    [RequireComponent(typeof(Collider))]
    public class LetterItem : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] Transform itemRoot;
        [SerializeField] Rigidbody letterRigidbody;
        [SerializeField] Collider[] letterColliders;
        [SerializeField] Vector3 carriedVisualOffset = new(0f, -0.08f, 0f);
        [SerializeField] float dropForwardOffset = 0.65f;
        [SerializeField] float dropVerticalOffset = -0.15f;
        [SerializeField] float dropVelocity = 1.5f;

        public bool IsCarried { get; protected set; }
        public bool IsOpenedAtDesk { get; protected set; }
        protected Transform ItemRoot => itemRoot != null ? itemRoot : transform;

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

            if (letterRigidbody == null)
            {
                letterRigidbody = GetComponentInChildren<Rigidbody>();
            }

            if (letterColliders == null || letterColliders.Length == 0)
            {
                letterColliders = GetComponentsInChildren<Collider>();
            }
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

            if (letterRigidbody != null)
            {
                letterRigidbody.linearVelocity = forwardDirection.normalized * dropVelocity;
            }
        }

        public virtual bool OpenAtDesk(Transform deskAnchor)
        {
            if (!IsCarried || deskAnchor == null)
            {
                return false;
            }

            PlaceAtDesk(deskAnchor);
            return true;
        }

        public bool CanOpenOutsideDesk()
        {
            return !IsOpenedAtDesk;
        }

        public void PlaceAtDesk(Transform deskAnchor)
        {
            PlaceAtDesk(deskAnchor, markOpenedAtDesk: true);
        }

        protected void PlaceAtDesk(Transform deskAnchor, bool markOpenedAtDesk)
        {
            EnsureInitialized();

            IsCarried = false;
            IsOpenedAtDesk = markOpenedAtDesk;

            itemRoot.SetParent(deskAnchor, worldPositionStays: false);
            itemRoot.localPosition = Vector3.zero;
            itemRoot.localRotation = Quaternion.identity;

            SetPhysicsForCarriedState();
        }

        protected void SetPhysicsForCarriedState()
        {
            if (letterRigidbody != null)
            {
                letterRigidbody.isKinematic = true;
                letterRigidbody.useGravity = false;
                letterRigidbody.linearVelocity = Vector3.zero;
                letterRigidbody.angularVelocity = Vector3.zero;
            }

            SetColliderTriggers(true);
        }

        protected void SetPhysicsForWorldState()
        {
            if (letterRigidbody != null)
            {
                letterRigidbody.isKinematic = false;
                letterRigidbody.useGravity = true;
            }

            SetColliderTriggers(false);
        }

        protected void SetColliderTriggers(bool value)
        {
            if (letterColliders == null)
            {
                return;
            }

            for (int i = 0; i < letterColliders.Length; i += 1)
            {
                if (letterColliders[i] != null)
                {
                    letterColliders[i].isTrigger = value;
                }
            }
        }

        Vector3 CalculateCarryLocalPosition()
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
