using System;
using UnityEngine;

namespace Whisperer
{
    public static class GameCursorController
    {
        static int modalUiDepth;

        public static bool IsModalUiActive => modalUiDepth > 0;

        public static event Action StateChanged;

        public static void PushModalUi()
        {
            modalUiDepth += 1;
            ApplyCursorState();
            StateChanged?.Invoke();
        }

        public static void PopModalUi()
        {
            if (modalUiDepth <= 0)
            {
                modalUiDepth = 0;
                ApplyCursorState();
                StateChanged?.Invoke();
                return;
            }

            modalUiDepth -= 1;
            ApplyCursorState();
            StateChanged?.Invoke();
        }

        public static void ApplyGameplayCursor(bool shouldLock)
        {
            if (IsModalUiActive)
            {
                ApplyCursorState();
                return;
            }

            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }

        static void ApplyCursorState()
        {
            if (IsModalUiActive)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}