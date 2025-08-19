using UnityEngine;
using Drawing;

namespace LNE.Utilities
{
    public enum RaycastDirection
    {
        Forward,
        Right,
        Up,
        Backward,
        Left,
        Down
    }

    public class RaycastSensor
    {
        public float CastLength { get; set; } = 1f;
        public LayerMask LayerMask { get; set; } = -1;

        private readonly Transform _transform;
        private Vector3 _origin;
        private RaycastDirection _raycastDirection;
        private RaycastHit _hitInfo;

        public RaycastSensor(Transform playerTransform)
        {
            _transform = playerTransform;
        }

        public void Cast()
        {
            Vector3 worldOrigin = _transform.TransformPoint(_origin);
            Vector3 worldDirection = GetCastDirection();

            Physics.Raycast(
                worldOrigin,
                worldDirection,
                out _hitInfo,
                CastLength,
                LayerMask,
                QueryTriggerInteraction.Ignore
            );
        }

        public bool HasDetectedHit() => _hitInfo.collider is not null;
        public float GetDistance() => _hitInfo.distance;
        public Vector3 GetNormal() => _hitInfo.normal;
        public Vector3 GetPosition() => _hitInfo.point;
        public Collider GetCollider() => _hitInfo.collider;
        public Transform GetTransform() => _hitInfo.transform;

        public void SetCastDirection(RaycastDirection direction) => _raycastDirection = direction;
        public void SetCastOrigin(Vector3 pos) => _origin = _transform.InverseTransformPoint(pos);

        private Vector3 GetCastDirection()
        {
            return _raycastDirection switch
            {
                RaycastDirection.Forward => _transform.forward,
                RaycastDirection.Right => _transform.right,
                RaycastDirection.Up => _transform.up,
                RaycastDirection.Backward => -_transform.forward,
                RaycastDirection.Left => -_transform.right,
                RaycastDirection.Down => -_transform.up,
                _ => Vector3.one
            };
        }

        public void DrawDebug()
        {
            if (!HasDetectedHit())
            {
                return;
            }

            Draw.Ray(_hitInfo.point, _hitInfo.normal, Color.red);
            const float markerSize = 0.2f;
            Draw.Line(
                _hitInfo.point + Vector3.right * markerSize,
                _hitInfo.point - Vector3.right * markerSize,
                Color.red
            );
            Draw.Line(
                _hitInfo.point + Vector3.forward * markerSize,
                _hitInfo.point - Vector3.forward * markerSize,
                Color.red
            );
            Draw.SolidCircle(_hitInfo.point, _hitInfo.normal, 0.1f, Color.green);
        }
    }
}