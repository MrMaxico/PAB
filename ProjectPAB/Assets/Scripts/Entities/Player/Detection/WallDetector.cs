using UnityEngine;

namespace Entities.Player.Detection
{
    public class WallDetector : BaseDetector<WallCheck>
    {
        [Header("References")]
        [SerializeField] private Transform _playerObject;

        [Header("Settings")]
        [SerializeField] private LayerMask _wallLayer;
        [SerializeField] private float _wallJumpGracePeriod = 0.1f;
        [SerializeField] private float _originHeight = 0.5f;

        // --- Results ---
        public Vector3 WallNormal { get; private set; }
        public Vector3 WallForward { get; private set; }
        public RaycastHit WallHit { get; private set; }

        protected override LayerMask DefaultLayerMask => _wallLayer;

        // --- Internal ---
        private float _lastWallJumpTime;

        private Vector3 RayOrigin => transform.position + Vector3.up * _originHeight;

        // ─── Registration ─── \\

        /// <summary>
        /// Register a wall check. Lower <paramref name="priority"/> wins when
        /// multiple checks hit (0 = highest priority).
        /// Omit <paramref name="layerMask"/> to use the detector's default wall layer.
        /// Set <paramref name="triggerInteraction"/> to <see cref="QueryTriggerInteraction.Collide"/>
        /// to detect trigger colliders (e.g. climbable surfaces).
        /// </summary>
        public void AddCheck(string id, Vector3 direction, float distance, int priority,
            CastType type, float radius = 0, LayerMask layerMask = default,
            QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
        {
            var check = new WallCheck(id, direction, distance, priority, type, radius,
                ResolveLayerMask(layerMask), triggerInteraction);
            RegisterCheck(check);
        }

        // ─── Tick ─── \\

        /// <summary>
        /// Casts all registered checks and derives wall data from the best hit.
        /// Call once per FixedUpdate.
        /// </summary>
        public void Tick()
        {
            if (IsInWallJumpGracePeriod())
            {
                ClearChecks();
                ResetWall();
                return;
            }

            CastRegisteredChecks();

            if (TryGetBestHit(out RaycastHit bestHit))
            {
                WallNormal = bestHit.normal;
                WallHit = bestHit;
                WallForward = Vector3.Cross(WallNormal, Vector3.up);

                if (Vector3.Dot(_playerObject.forward, WallForward) < 0)
                    WallForward = -WallForward;
            }
            else
            {
                ResetWall();
                ClearChecks();
            }
        }

        public void RegisterJumpTime() => _lastWallJumpTime = Time.time;

        // ─── Private helpers ─── \\

        private bool IsInWallJumpGracePeriod()
        {
            return Time.time - _lastWallJumpTime < _wallJumpGracePeriod;
        }

        private void CastRegisteredChecks()
        {
            Vector3 origin = RayOrigin;

            for (int i = 0; i < Checks.Count; i++)
            {
                WallCheck check = Checks[i];
                Vector3 worldDir = _playerObject.TransformDirection(check.Direction);

                bool hit;
                RaycastHit hitInfo;
                Ray ray = new(origin, worldDir);

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
        /// Returns the highest-priority hit.
        /// </summary>
        private bool TryGetBestHit(out RaycastHit bestHit)
        {
            for (int i = 0; i < Checks.Count; i++)
            {
                if (Checks[i].IsHit)
                {
                    bestHit = Checks[i].HitInfo;
                    return true;
                }
            }
            bestHit = default;
            return false;
        }

        private void ResetWall()
        {
            WallNormal = Vector3.zero;
            WallForward = Vector3.zero;
            WallHit = default;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_playerObject == null) return;

            Vector3 origin = RayOrigin;

            for (int i = 0; i < Checks.Count; i++)
            {
                WallCheck check = Checks[i];
                Vector3 worldDir = _playerObject.TransformDirection(check.Direction);

                Gizmos.color = check.IsHit ? Color.green : Color.red;
                Gizmos.DrawRay(origin, worldDir * check.Distance);

                if (check.Type == CastType.SphereCast)
                {
                    Gizmos.DrawWireSphere(origin + worldDir * check.Distance, check.Radius);
                }
            }

            if (WallNormal == Vector3.zero) return;

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(WallHit.point, 0.05f);
            Gizmos.DrawRay(WallHit.point, WallNormal * 0.5f);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(origin, WallForward * 1.0f);
        }
#endif
    }
}