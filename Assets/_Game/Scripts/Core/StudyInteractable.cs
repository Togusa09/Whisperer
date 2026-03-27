using UnityEngine;

namespace Whisperer
{
    public abstract class StudyInteractable : MonoBehaviour
    {
        public virtual string InteractionPrompt => "Interact";
        public virtual string GetInteractionPrompt(PlayerInteractionController controller) => InteractionPrompt;
        public virtual bool CanInteractWhileCarrying(PlayerInteractionController controller) => false;
        public abstract bool TryInteract(PlayerInteractionController controller);
    }
}
