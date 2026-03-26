using UnityEngine;

namespace Whisperer
{
    public class DeskPropItem : CarriableItem
    {
        public bool IsPlacedAtDesk { get; private set; }

        public override void PickUp(Transform carryAnchor)
        {
            base.PickUp(carryAnchor);
            IsPlacedAtDesk = false;
        }

        public override void Drop(Vector3 forwardDirection)
        {
            base.Drop(forwardDirection);
            IsPlacedAtDesk = false;
        }

        public void PlaceAtDesk(Transform deskAnchor)
        {
            if (deskAnchor == null)
            {
                return;
            }

            PlaceAtDeskCore(deskAnchor);
            IsPlacedAtDesk = true;
        }

        public void PlaceAtDesk(Vector3 worldPosition, Quaternion worldRotation, Transform deskParent = null)
        {
            PlaceAtDeskCore(worldPosition, worldRotation, deskParent);
            IsPlacedAtDesk = true;
        }
    }
}
