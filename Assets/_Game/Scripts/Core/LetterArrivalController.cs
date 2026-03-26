using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Whisperer
{
    public class LetterArrivalController : MonoBehaviour
    {
        const string PreferredSpawnPointName = "EnvelopeSpawn";

        [Header("References")]
        [FormerlySerializedAs("letterPrefab")]
        [SerializeField] GameObject mailPrefab;
        [FormerlySerializedAs("mailslotSpawnPoint")]
        [SerializeField] Transform spawnPoint;

        [Header("Spawn Tuning")]
        [SerializeField] float forwardImpulse = 0.3f;
        [SerializeField] float downwardImpulse = 0.6f;
        [SerializeField] float minSpawnInterval = 0.5f;
        [SerializeField] bool spawnWithDebugHotkey = true;
        [SerializeField] Key debugSpawnKey = Key.R;

        float lastSpawnTime = -999f;

        void Awake()
        {
            NormalizeSerializedState();
        }

        void OnValidate()
        {
            NormalizeSerializedState();
        }

        void Update()
        {
            if (!spawnWithDebugHotkey)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && WasDebugSpawnPressed(keyboard))
            {
                bool spawned = SpawnIncomingLetter();
                Debug.Log(spawned
                    ? $"LetterArrivalController: Spawned debug letter with {debugSpawnKey}."
                    : $"LetterArrivalController: Debug spawn with {debugSpawnKey} was blocked.", this);
            }
        }

        bool WasDebugSpawnPressed(Keyboard keyboard)
        {
            return debugSpawnKey switch
            {
                Key.R => keyboard.rKey.wasPressedThisFrame,
                Key.E => keyboard.eKey.wasPressedThisFrame,
                Key.Space => keyboard.spaceKey.wasPressedThisFrame,
                Key.Enter => keyboard.enterKey.wasPressedThisFrame,
                _ => keyboard[debugSpawnKey].wasPressedThisFrame,
            };
        }

        public bool SpawnIncomingLetter()
        {
            if (mailPrefab == null)
            {
                Debug.LogWarning("LetterArrivalController: Mail prefab is not assigned.", this);
                return false;
            }

            if (spawnPoint == null)
            {
                Debug.LogWarning("LetterArrivalController: Spawn point is not assigned.", this);
                return false;
            }

            if (Time.time - lastSpawnTime < minSpawnInterval)
            {
                Debug.Log($"LetterArrivalController: Spawn blocked by cooldown ({minSpawnInterval:0.00}s).", this);
                return false;
            }

            lastSpawnTime = Time.time;

            GameObject mailItem = Instantiate(
                mailPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            mailItem.name = mailPrefab.name;

            Rigidbody body = mailItem.GetComponentInChildren<Rigidbody>();
            if (body != null)
            {
                Vector3 impulse = spawnPoint.forward * forwardImpulse + Vector3.down * downwardImpulse;
                body.AddForce(impulse, ForceMode.VelocityChange);
            }

            Debug.Log($"LetterArrivalController: Spawned {mailPrefab.name} at {spawnPoint.position}.", this);

            return true;
        }

        void NormalizeSerializedState()
        {
            if (debugSpawnKey == Key.F15)
            {
                debugSpawnKey = Key.R;
            }

            Transform preferredSpawn = transform.Find(PreferredSpawnPointName);
            if (preferredSpawn != null && (spawnPoint == null || spawnPoint.name != PreferredSpawnPointName))
            {
                spawnPoint = preferredSpawn;
            }

#if UNITY_EDITOR
            if (mailPrefab == null)
            {
                string envelopeGuid = AssetDatabase.FindAssets("Envelope t:Prefab")
                    .FirstOrDefault(guid => AssetDatabase.GUIDToAssetPath(guid).EndsWith("Envelope.prefab", System.StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(envelopeGuid))
                {
                    mailPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(envelopeGuid));
                }
            }
#endif
        }
    }
}
