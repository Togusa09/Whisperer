using UnityEngine;

namespace Whisperer
{
    public class PlayerSpawnMarker : MonoBehaviour
    {
        [Header("Spawn Orientation")]
        [Tooltip("If assigned, the player will face this transform's forward direction when spawned.")]
        public Transform facingTransform;

        public Vector3 GetSpawnPosition()
        {
            return transform.position;
        }

        public Quaternion GetSpawnRotation()
        {
            if (facingTransform != null)
            {
                return Quaternion.LookRotation(facingTransform.forward, Vector3.up);
            }

            return transform.rotation;
        }
    }
}
