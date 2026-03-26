using UnityEngine;

namespace Whisperer
{
    public abstract class StudyInteractable : MonoBehaviour
    {
        public abstract bool TryInteract(PlayerInteractionController controller);
    }
}
