using UnityEngine;

namespace Whisperer
{
    public abstract class StudyInteractable : MonoBehaviour
    {
        public virtual string InteractionPrompt => "Interact";
        public abstract bool TryInteract(PlayerInteractionController controller);
    }
}
