using UnityEngine;

namespace Entities.Player.Detection
{
    public class WaterDetector : BaseDetector<MovementCheck>
    {
        [SerializeField] private bool DoDebug;

        [Header("References")]
        [SerializeField] private Transform _playerObject;

        [Header("Settings")]
        [SerializeField] private LayerMask _detectionLayer;
        public LayerMask DetectionLayer => _detectionLayer;
        protected override LayerMask DefaultLayerMask => _detectionLayer;

        [SerializeField] private float _originOffset = 0.1f;

        // ─── Results ───
        public DetectionHit Hit { get; private set; }

        // How much of the body's vertical extent is below the water surface, 0-1.
        // 0 = not touching water, 1 = fully submerged. NaN-safe: stays 0 if no surface is found.
        public float SubmersionFraction { get; private set; }
        public bool HasWaterSurface { get; private set; }
        public float WaterSurfaceHeight { get; private set; }

        private Vector3 _lastMovementDirection;
        private Vector3 RayOrigin => transform.position + Vector3.up * _originOffset;

        // ─── Registration shorthands ───

        public void AddRay(string id, Vector3 direction, float distance, int priority = 0)
        {
            var check = MovementCheck.Ray(id, direction, distance, priority);
            check.IncludeTriggers();
            AddCheck(check);
        }

        public void AddSphere(string id, Vector3 direction, float distance, float radius, int priority = 0)
        {
            var check = MovementCheck.Sphere(id, direction, distance, radius, priority);
            check.IncludeTriggers();
            AddCheck(check);
        }

        public void AddMovementRay(string id, float distance, int priority = 0)
        {
            var check = MovementCheck.Movement(id, distance, priority);
            check.IncludeTriggers();
            AddCheck(check);
        }

        public void AddMovementSphere(string id, float distance, float radius, int priority = 0)
        {
            var check = MovementCheck.Movement(id, distance, priority);
            check.AsSphere(radius);
            check.IncludeTriggers();
            AddCheck(check);
        }

        // ─── Tick ───

        public void Tick(Vector3 movementDirection = new())
        {
            _lastMovementDirection = movementDirection.normalized;

            CastChecks(RayOrigin, check =>
                check.UseMovementDirection
                    ? _lastMovementDirection
                    : _playerObject.TransformDirection(check.Direction));

            if (TryGetBestHit(out RaycastHit rawHit))
            {
                Hit = new DetectionHit(rawHit);
            }
            else
            {
                Hit = DetectionHit.None;
                ResetHits();
            }
        }

        // ─── Submersion ───
        // only works when entering water from above
        public void TickSubmersion(Collider bodyCollider)
        {
            Bounds bounds = bodyCollider.bounds;
            float bodyBottomY = bounds.min.y;
            float bodyTopY = bounds.max.y;
            float bodyHeight = bodyTopY - bodyBottomY;

            if (bodyHeight <= 0f)
            {
                HasWaterSurface = false;
                SubmersionFraction = 0f;
                return;
            }

            Vector3 castOrigin = new(transform.position.x, bodyTopY + 0.25f, transform.position.z);
            float castDistance = bodyHeight + 0.5f;

            if (Physics.Raycast(castOrigin, Vector3.down, out RaycastHit hit, castDistance, _detectionLayer, QueryTriggerInteraction.Collide))
            {
                HasWaterSurface = true;
                WaterSurfaceHeight = hit.point.y;

                float submergedHeight = Mathf.Clamp(hit.point.y - bodyBottomY, 0f, bodyHeight);
                SubmersionFraction = submergedHeight / bodyHeight;
            }
            else
            {
                HasWaterSurface = false;
                SubmersionFraction = 0f;
            }
        }

        // ─── Private ───

        private bool TryGetBestHit(out RaycastHit bestHit)
        {
            for (int i = 0; i < Checks.Count; i++)
            {
                if (Checks[i].IsHit)
                {
                    bestHit = Checks[i].Hit;
                    return true;
                }
            }

            bestHit = default;
            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!DoDebug) return;

            if (_playerObject == null) return;

            Vector3 origin = RayOrigin;

            for (int i = 0; i < Checks.Count; i++)
            {
                MovementCheck check = Checks[i];
                Vector3 worldDir = check.UseMovementDirection
                    ? (_lastMovementDirection != Vector3.zero ? _lastMovementDirection : _playerObject.forward)
                    : _playerObject.TransformDirection(check.Direction);

                Gizmos.color = check.IsHit ? Color.cyan : Color.blue;
                Gizmos.DrawRay(origin, worldDir * check.Distance);

                if (check.CastType == CastType.SphereCast)
                    Gizmos.DrawWireSphere(origin + worldDir * check.Distance, check.Radius);
            }

            if (!Hit.IsHit) return;

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(Hit.Point, 0.05f);
        }
#endif
    }
}
