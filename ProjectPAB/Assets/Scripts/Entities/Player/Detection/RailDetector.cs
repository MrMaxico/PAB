using UnityEngine;

namespace Entities.Player.Detection
{
    public class RailDetector : BaseDetector<RailCheck>
    {
        [Header("Settings")]
        [SerializeField] private LayerMask _railLayer;
        [SerializeField] private float _maxSlopeAngle = 45f;
        [SerializeField] private float _originOffset = 0.1f;

        [Header("Timing")]
        [SerializeField] private float _railedGracePeriod = 0.3f;
        [SerializeField] private float _coyoteTime = 0.5f;

        // --- Results ---
        public Vector3 Normal { get; private set; } = Vector3.up;
        public float Angle { get; private set; }
        public LayerMask RailLayer => _railLayer;

        private bool _isSloped;
        public bool IsSloped => _isSloped;

        [SerializeField] private float _coyoteTimeCounter;
        public float CoyoteTimeCounter => _coyoteTimeCounter;

        protected override LayerMask DefaultLayerMask => _railLayer;

        // --- Internal ---
        private float _lastJumpTime;

        private Vector3 RayOrigin => transform.position + Vector3.up * _originOffset;

        // ─── Registration ─── \\

        /// <summary>
        /// Register a ground check. Lower <paramref name="priority"/> wins when
        /// multiple checks hit (0 = highest priority).
        /// Omit <paramref name="layerMask"/> to use the detector's default ground layer.
        /// Set <paramref name="triggerInteraction"/> to <see cref="QueryTriggerInteraction.Collide"/>
        /// to detect trigger colliders (e.g. intangible platforms).
        /// </summary>
        public void AddCheck(string id, Vector3 direction, float distance, int priority, CastType type, float radius = 0, LayerMask layerMask = default, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
        {
            var check = new RailCheck(id, direction, distance, priority, type, radius, ResolveLayerMask(layerMask), triggerInteraction);
            RegisterCheck(check);
        }

        // ─── Tick ─── \\

        /// <summary>
        /// Casts all registered checks and derives slope data from the best walkable hit.
        /// Call once per FixedUpdate.
        /// </summary>
        public void Tick()
        {
            if (IsInJumpGracePeriod())
            {
                ClearChecks();
                ResetSlope();
                _coyoteTimeCounter = 0;
                return;
            }

            CastRegisteredChecks();

            if (TryGetBestWalkableHit(out RaycastHit bestHit))
            {
                Normal = bestHit.normal;
                Angle = Vector3.Angle(Vector3.up, bestHit.normal);
                _isSloped = Angle > 0.1f;
                _coyoteTimeCounter = _coyoteTime;
            }
            else
            {
                ResetSlope();
                _coyoteTimeCounter -= Time.deltaTime;
                ClearChecks();
            }
        }

        public void RegisterJumpTime() => _lastJumpTime = Time.time;

        public void ResetCoyoteTime() => _coyoteTimeCounter = 0f;

        public void ForceCheck() => Tick();

        // ─── Private helpers ─── \\

        private bool IsInJumpGracePeriod()
        {
            return Time.time - _lastJumpTime < _railedGracePeriod;
        }

        private void CastRegisteredChecks()
        {
            Vector3 origin = RayOrigin;

            for (int i = 0; i < Checks.Count; i++)
            {
                RailCheck check = Checks[i];

                bool hit;
                RaycastHit hitInfo;
                Ray ray = new(origin, check.Direction);

                if (check.Type == CastType.SphereCast)
                {
                    hit = Physics.SphereCast(ray, check.Radius, out hitInfo,
                        check.Distance, check.LayerMask, check.TriggerInteraction);
                }
                else
                {
                    hit = Physics.Raycast(ray, out hitInfo,
                        check.Distance, check.LayerMask, check.TriggerInteraction);
                }

                check.IsHit = hit;
                check.HitInfo = hitInfo;
            }
        }

        /// <summary>
        /// Returns the highest-priority hit that lands on a walkable surface.
        /// </summary>
        private bool TryGetBestWalkableHit(out RaycastHit bestHit)
        {
            for (int i = 0; i < Checks.Count; i++)
            {
                if (Checks[i].IsHit && IsWalkableSurface(Checks[i].HitInfo.normal))
                {
                    bestHit = Checks[i].HitInfo;
                    return true;
                }
            }
            bestHit = default;
            return false;
        }

        private bool IsWalkableSurface(Vector3 normal)
        {
            return Vector3.Angle(Vector3.up, normal) <= _maxSlopeAngle;
        }

        private void ResetSlope()
        {
            _isSloped = false;
            Normal = Vector3.up;
            Angle = 0f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 origin = RayOrigin;

            for (int i = 0; i < Checks.Count; i++)
            {
                RailCheck check = Checks[i];

                Gizmos.color = check.IsHit ? Color.green : Color.red;
                Gizmos.DrawRay(origin, check.Direction * check.Distance);

                if (check.Type == CastType.SphereCast)
                {
                    Gizmos.DrawWireSphere(origin + check.Direction * check.Distance, check.Radius);
                }
            }

            if (_isSloped)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, Normal * 1.5f);
            }
        }
#endif
    }
}