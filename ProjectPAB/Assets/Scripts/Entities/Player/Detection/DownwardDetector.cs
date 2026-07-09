using UnityEngine;

namespace Entities.Player.Detection
{
    public abstract class DownwardDetector : BaseDetector<DetectionCheck>
    {
        [SerializeField] private bool DoDebug;

        [Header("Settings")]
        [SerializeField] private LayerMask _detectionLayer;
        public LayerMask DetectionLayer => _detectionLayer;
        protected override LayerMask DefaultLayerMask => _detectionLayer;

        [SerializeField] private float _maxSlopeAngle = 45f;
        public float MaxSlopeAngle => _maxSlopeAngle;

        [Header("Timing")]
        [SerializeField] private float _coyoteTime = 0.5f;

        // --- Results ---
        public DetectionHit Hit { get; private set; }
        public bool HasHit => Hit.IsHit;
        [SerializeField] private float _coyoteTimeCounter;
        public float CoyoteTimeCounter => _coyoteTimeCounter;

        #region Registration

        public void AddRay(string id, float distance, int priority = 0) =>
            AddCheck(DetectionCheck.Ray(id, Vector3.down, distance, priority));

        public void AddSphere(string id, float distance, float radius, int priority = 0) =>
            AddCheck(DetectionCheck.Sphere(id, Vector3.down, distance, radius, priority));

        #endregion

        #region Tick Hooks

        protected override void ClearHit() => Hit = DetectionHit.None;

        protected override void OnHit(RaycastHit rawHit)
        {
            float slopeAngle = Vector3.Angle(Vector3.up, rawHit.normal);
            Hit = new DetectionHit(rawHit, slopeAngle);
            _coyoteTimeCounter = _coyoteTime;
        }

        protected override void OnMiss() => _coyoteTimeCounter -= Time.deltaTime;

        protected override void OnGraceTick() => _coyoteTimeCounter = 0f;

        public void ResetCoyoteTime() => _coyoteTimeCounter = 0f;

        #endregion

        #region Helpers

        // Prefers hits within the slope limit, falls back to any hit.
        protected override bool TryGetBestHit(out RaycastHit bestHit)
        {
            foreach (var check in Checks)
            {
                if (check.IsHit && Vector3.Angle(Vector3.up, check.Hit.normal) <= _maxSlopeAngle)
                {
                    bestHit = check.Hit;
                    return true;
                }
            }

            return base.TryGetBestHit(out bestHit);
        }

        #endregion

        #region Gizmos
#if UNITY_EDITOR

        protected virtual void OnDrawGizmosSelected()
        {
            if (!DoDebug) return;

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
        #endregion
    }
}
