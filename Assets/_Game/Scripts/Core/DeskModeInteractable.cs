using UnityEngine;

namespace Whisperer
{
    public class DeskModeInteractable : StudyInteractable
    {
        [SerializeField] PlayerModeSwitcher modeSwitcher;

        void Awake()
        {
            if (modeSwitcher == null)
            {
                modeSwitcher = FindAnyObjectByType<PlayerModeSwitcher>();
            }
        }

        public override bool TryInteract(PlayerInteractionController controller)
        {
            if (modeSwitcher == null)
            {
                modeSwitcher = FindAnyObjectByType<PlayerModeSwitcher>();
                if (modeSwitcher == null)
                {
                    Debug.LogWarning("DeskModeInteractable: Mode switcher not assigned.", this);
                    return false;
                }
            }

            modeSwitcher.EnterDeskMode();
            return true;
        }
    }
}
