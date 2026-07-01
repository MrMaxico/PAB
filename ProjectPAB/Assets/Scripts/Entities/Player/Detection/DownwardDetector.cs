using UnityEngine;

namespace Entities.Player.Detection
{
    public abstract class DownwardDetector : BaseDetector<DetectionCheck>
    {
        [Header("Settings")]
        [SerializeField] public LayerMask _detectionLayer;
        [SerializeField] private float _maxSlopeAngle = 45f;
        [SerializeField] private float _originOffset = 0.1f;

        [Header("Timing")]
        [SerializeField] private float _gracePeriod = 0.3f;
        [SerializeField] private float _coyoteTime = 0.5f;

        // --- Results ---
        public DetectionHit Hit { get; private set; }
        [SerializeField] private float _coyoteTimeCounter;
        public float CoyoteTimeCounter => _coyoteTimeCounter;

        public LayerMask DetectionLayer => _detectionLayer;
        protected override LayerMask DefaultLayerMask => _detectionLayer;

        private float _lastJumpTime;
        private Vector3 RayOrigin => transform.position + Vector3.up * _originOffset;

        // ─── Registration shorthands ───

        public void AddRay(string id, float distance, int priority = 0) =>
            AddCheck(DetectionCheck.Ray(id, Vector3.down, distance, priority));

        public void AddSphere(string id, float distance, float radius, int priority = 0) =>
            AddCheck(DetectionCheck.Sphere(id, Vector3.down, distance, radius, priority));

        // ─── Tick ───

        public void Tick()
        {
            if (Time.time - _lastJumpTime < _gracePeriod)
            {
                ResetHits();
                Hit = DetectionHit.None;
                _coyoteTimeCounter = 0f;
                return;
            }

            CastChecks(RayOrigin, check => check.Direction);

            if (TryGetBestHit(out RaycastHit rawHit))
            {
                float slopeAngle = Vector3.Angle(Vector3.up, rawHit.normal);
                Hit = new DetectionHit(rawHit, slopeAngle);
                _coyoteTimeCounter = _coyoteTime;
            }
            else
            {
                Hit = DetectionHit.None;
                _coyoteTimeCounter -= Time.deltaTime;
                ResetHits();
            }
        }

        public void RegisterJumpTime() => _lastJumpTime = Time.time;
        public void ResetCoyoteTime() => _coyoteTimeCounter = 0f;

        // ─── Private ───

        private bool TryGetBestHit(out RaycastHit bestHit)
        {
            for (int i = 0; i < Checks.Count; i++)
            {
                if (Checks[i].IsHit && Vector3.Angle(Vector3.up, Checks[i].Hit.normal) <= _maxSlopeAngle)
                {
                    bestHit = Checks[i].Hit;
                    return true;
                }
            }
            bestHit = default;
            return false;
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            Vector3 origin = RayOrigin;

            for (int i = 0; i < Checks.Count; i++)
            {
                DetectionCheck check = Checks[i];
                Gizmos.color = check.IsHit ? Color.green : Color.red;
                Gizmos.DrawRay(origin, check.Direction * check.Distance);

                if (check.CastType == CastType.SphereCast)
                    Gizmos.DrawWireSphere(origin + check.Direction * check.Distance, check.Radius);
            }

            if (Hit.IsSloped)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, Hit.Normal * 1.5f);
            }
        }
#endif
    }
}
