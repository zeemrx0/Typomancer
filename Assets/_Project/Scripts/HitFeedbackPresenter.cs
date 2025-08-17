using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LNE
{
    public class HitFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private MMF_Player _mmfPlayer;

        [Button]
        public void Hit(Vector3 direction)
        {
            SetHitDirection(direction);
            _mmfPlayer.PlayFeedbacks();
        }

        public void SetHitDirection(Vector3 direction)
        {
            MMF_RotationSpring rotationSpringFeedback = _mmfPlayer.GetFeedbackOfType<MMF_RotationSpring>();

            // Calculate rotation components for each axis based on the hit direction
            Vector3 bumpRotation = Vector3.zero;

            // X-axis component (left/right hits) - rotate around Z-axis
            if (Mathf.Abs(direction.x) > 0.01f)
            {
                float xRotationSign =
                    direction.x > 0 ? -1f : 1f; // Right hit rotates clockwise, left hit rotates counter-clockwise
                bumpRotation.z += xRotationSign * Mathf.Abs(direction.x) * 80f;
            }

            // Z-axis component (forward/backward hits) - rotate around X-axis
            if (Mathf.Abs(direction.z) > 0.01f)
            {
                float zRotationSign = direction.z > 0 ? 1f : -1f; // Forward hit rotates down, backward hit rotates up
                bumpRotation.x += zRotationSign * Mathf.Abs(direction.z) * 80f;
            }

            // Y-axis component (up/down hits) - rotate around X-axis (additive with Z component)
            if (Mathf.Abs(direction.y) > 0.01f)
            {
                float yRotationSign = direction.y > 0 ? -1f : 1f; // Up hit rotates backward, down hit rotates forward
                bumpRotation.x += yRotationSign * Mathf.Abs(direction.y) * 80f;
            }

            rotationSpringFeedback.BumpRotationMin = bumpRotation;
            rotationSpringFeedback.BumpRotationMax = bumpRotation;
        }
    }
}