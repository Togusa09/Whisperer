using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Whisperer
{
    public class PlayerModeSwitcher : MonoBehaviour
    {
        [Header("Startup")]
        [SerializeField] PlayerMode startMode = PlayerMode.Explore;
        [SerializeField] bool placePlayerAtSpawnOnStart = true;

        [Header("References")]
        [SerializeField] Transform playerRoot;
        [SerializeField] Camera explorationCamera;
        [SerializeField] Camera deskCamera;
        [SerializeField] PlayerSpawnMarker spawnMarker;

        [Header("Enable During Explore")]
        [SerializeField] MonoBehaviour[] exploreModeBehaviours;

        [Header("Enable During Desk")]
        [SerializeField] MonoBehaviour[] deskModeBehaviours;

        public PlayerMode CurrentMode { get; private set; }
        public Camera ExplorationCamera => explorationCamera;
        public Camera DeskCamera => deskCamera;
        public Camera ActiveCamera => CurrentMode == PlayerMode.Desk ? deskCamera : explorationCamera;

        public event Action<PlayerMode> ModeChanged;

        void Awake()
        {
            if (spawnMarker == null)
            {
                spawnMarker = FindAnyObjectByType<PlayerSpawnMarker>();
            }
        }

        void Start()
        {
            if (placePlayerAtSpawnOnStart)
            {
                PlacePlayerAtSpawn();
            }

            SetMode(startMode, force: true);
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (CurrentMode == PlayerMode.Desk && keyboard.escapeKey.wasPressedThisFrame)
            {
                EnterExploreMode();
            }
        }

        public void EnterDeskMode()
        {
            SetMode(PlayerMode.Desk);
        }

        public void EnterExploreMode()
        {
            SetMode(PlayerMode.Explore);
        }

        public void ToggleMode()
        {
            SetMode(CurrentMode == PlayerMode.Explore ? PlayerMode.Desk : PlayerMode.Explore);
        }

        public void PlacePlayerAtSpawn()
        {
            if (playerRoot == null)
            {
                Debug.LogWarning("PlayerModeSwitcher: Cannot place player at spawn because Player Root is not assigned.", this);
                return;
            }

            if (spawnMarker == null)
            {
                Debug.LogWarning("PlayerModeSwitcher: No PlayerSpawnMarker found in scene.", this);
                return;
            }

            Vector3 spawnPosition = spawnMarker.GetSpawnPosition();
            CharacterController characterController = playerRoot.GetComponent<CharacterController>();
            if (characterController != null)
            {
                // Treat the marker as the intended capsule center so spawn points can be authored at standing height.
                spawnPosition -= Vector3.up * characterController.center.y;
            }

            playerRoot.SetPositionAndRotation(
                spawnPosition,
                spawnMarker.GetSpawnRotation()
            );
        }

        void SetMode(PlayerMode mode, bool force = false)
        {
            if (!force && mode == CurrentMode)
            {
                return;
            }

            CurrentMode = mode;

            bool isExploreMode = mode == PlayerMode.Explore;
            SetCameraState(explorationCamera, isExploreMode);
            SetCameraState(deskCamera, !isExploreMode);

            SetBehavioursEnabled(exploreModeBehaviours, isExploreMode);
            SetBehavioursEnabled(deskModeBehaviours, !isExploreMode);

            ModeChanged?.Invoke(mode);
        }

        static void SetCameraState(Camera targetCamera, bool enabled)
        {
            if (targetCamera == null)
            {
                return;
            }

            targetCamera.enabled = enabled;
            targetCamera.gameObject.SetActive(enabled);
        }

        static void SetBehavioursEnabled(MonoBehaviour[] behaviours, bool enabled)
        {
            if (behaviours == null)
            {
                return;
            }

            for (int i = 0; i < behaviours.Length; i += 1)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null)
                {
                    behaviour.enabled = enabled;
                }
            }
        }
    }
}
