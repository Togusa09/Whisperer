using UnityEngine;

namespace Whisperer
{
    public class LetterItem : CarriableItem
    {
        public bool IsOpenedAtDesk { get; protected set; }
        public bool IsPlacedAtDesk => IsOpenedAtDesk;

        public virtual bool OpenAtDesk(Transform deskAnchor)
        {
            if (!IsCarried || deskAnchor == null)
            {
                return false;
            }

            PlaceAtDesk(deskAnchor);
            return true;
        }

        public virtual bool CanPickUpAtDesk()
        {
            return IsOpenedAtDesk && !IsCarried;
        }

        public virtual bool PickUpFromDesk(Transform carryAnchor)
        {
            if (!CanPickUpAtDesk() || carryAnchor == null)
            {
                return false;
            }

            PickUp(carryAnchor);
            IsOpenedAtDesk = true;
            return true;
        }

        public bool CanOpenOutsideDesk()
        {
            return !IsOpenedAtDesk;
        }

        public void PlaceAtDesk(Transform deskAnchor)
        {
            PlaceAtDeskCore(deskAnchor);
            IsOpenedAtDesk = true;
        }

        public void PlaceAtDesk(Vector3 worldPosition, Quaternion worldRotation, Transform deskParent = null)
        {
            PlaceAtDeskCore(worldPosition, worldRotation, deskParent);
            IsOpenedAtDesk = true;
        }

        protected override void OnStoredInDrawer()
        {
            IsOpenedAtDesk = false;
        }

        protected override void OnRetrievedFromDrawer()
        {
            IsOpenedAtDesk = false;
        }
    }
}
