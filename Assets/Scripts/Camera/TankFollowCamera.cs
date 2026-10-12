using UnityEngine;

namespace NetworkTankArena.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class TankFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 12f, -8f);
        [SerializeField] private float lookAtHeight = 1.35f;

        private void Awake()
        {
            if (target == null || transform == target || transform.IsChildOf(target) ||
                (followOffset - Vector3.up * lookAtHeight).sqrMagnitude < 0.0001f ||
                new Vector2(followOffset.x, followOffset.z).sqrMagnitude < 0.0001f)
            {
                Debug.LogError("TankFollowCamera needs an independent gameplay-root target and an offset " +
                    "with horizontal separation from that target.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            // All Start calls precede the first Update, so mouse aiming starts with this pose.
            FollowTarget();
        }

        private void LateUpdate()
        {
            FollowTarget();
        }

        private void FollowTarget()
        {
            if (target == null)
                return;

            transform.position = target.position + followOffset;
            transform.rotation = Quaternion.LookRotation(Vector3.up * lookAtHeight - followOffset, Vector3.up);
        }
    }
}
