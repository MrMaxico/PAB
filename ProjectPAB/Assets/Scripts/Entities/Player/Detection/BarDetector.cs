using UnityEngine;
using UnityEngine.Serialization;

namespace Entities.Player.Detection
{
    public class BarDetector : BaseDetector<DetectionCheck>
    {
        [SerializeField] private bool DoDebug;

        [Header("Settings")]
        [FormerlySerializedAs("_barLayer")]
        [SerializeField] private LayerMask _detectionLayer;
        public LayerMask DetectionLayer => _detectionLayer;
        protected override LayerMask DefaultLayerMask => _detectionLayer;

        [SerializeField] private bool _includeTriggers = true;

        [SerializeField] private int _maxOverlaps = 8;

        [Header("Timing")]
        [SerializeField] private float _sameBarGracePeriod = 0.8f;

        private GameObject _previousBar;

        private Collider[] _hitColliders;

        public BarHit Hit { get; private set; }
        public bool HasHit => Hit.IsHit;

        #region Registration

        public void AddRay(string id, float distance, int priority = 0)
        {
            var check = DetectionCheck.Ray(id, Vector3.zero, distance, priority);
            if (_includeTriggers) check.IncludeTriggers();
            AddCheck(check);
        }

        public void AddSphere(string id, float distance, float radius, int priority = 0)
        {
            var check = DetectionCheck.Sphere(id, Vector3.zero, distance, radius, priority);
            if (_includeTriggers) check.IncludeTriggers();
            AddCheck(check);
        }

        #endregion

        #region Tick Hooks

        public override void Tick(Vector3 movementDirection = default)
        {
            if (!HasChecks)
            {
                ClearHit();
                return;
            }

            if (InJumpGrace)
            {
                ResetHits();
                ClearHit();
                return;
            }

            if (TryFindBar(RayOrigin, SearchRadius(), out BarHit hit)
                && !(hit.GameObject != null && IsSameBarBlocked(hit.GameObject)))
            {
                Hit = hit;
                MarkChecksHit(hit.RawHit); // keep HasAnyHit()/IsHit(id) in sync with HasHit
            }
            else
            {
                ClearHit();
                ResetHits();
            }
        }

        // The checks never cast themselves (the bar is found by proximity), so mirror the
        // result onto them for the base queries (HasAnyHit, IsHit, TryGetHit).
        private void MarkChecksHit(RaycastHit rawHit)
        {
            for (int i = 0; i < Checks.Count; i++)
            {
                Checks[i].IsHit = true;
                Checks[i].Hit = rawHit;
            }
        }

        protected override void ClearHit() => Hit = BarHit.None;

        protected override void OnHit(RaycastHit rawHit)
        {
            ResolveBarGeometry(rawHit.collider, RayOrigin, out Vector3 grabPoint, out Vector3 axis);
            Hit = new BarHit(rawHit, grabPoint, axis, RayOrigin - grabPoint);
        }

        public void RegisterJumpTime(GameObject releasedBar)
        {
            RegisterJumpTime();
            if (releasedBar != null) _previousBar = releasedBar;
        }

        private bool IsSameBarBlocked(GameObject bar) =>
            bar == _previousBar && Time.time - LastJumpTime < _sameBarGracePeriod;

        #endregion

        #region Attached Tracking

        public bool TryFindBar(Vector3 origin, float radius, out BarHit result)
        {
            if (!HasChecks)
            {
                result = BarHit.None;
                return false;
            }

            _hitColliders ??= new Collider[Mathf.Max(1, _maxOverlaps)];

            QueryTriggerInteraction triggers = _includeTriggers
                ? QueryTriggerInteraction.Collide
                : QueryTriggerInteraction.Ignore;

            int count = Physics.OverlapSphereNonAlloc(origin, radius, _hitColliders, _detectionLayer, triggers);
            if (count == 0)
            {
                result = BarHit.None;
                return false;
            }

            Collider nearest = null;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                float d = (ClosestPointSafe(_hitColliders[i], origin) - origin).sqrMagnitude;
                if (d < nearestSqr) { nearestSqr = d; nearest = _hitColliders[i]; }
            }

            ResolveBarGeometry(nearest, origin, out Vector3 grabPoint, out Vector3 axis);

            Vector3 toBar = grabPoint - origin;
            if (Physics.Raycast(origin, toBar.normalized, out RaycastHit rayHit, toBar.magnitude + 0.5f, _detectionLayer, triggers)
                && rayHit.collider == nearest)
            {
                result = new BarHit(rayHit, grabPoint, axis, -toBar);
            }
            else
            {
                result = new BarHit(default, grabPoint, axis, -toBar);
            }
            return true;
        }

        #endregion

        #region Helpers

        // Search range = the widest registered check (distance + sphere radius).
        private float SearchRadius()
        {
            float radius = 0f;
            for (int i = 0; i < Checks.Count; i++)
                radius = Mathf.Max(radius, Checks[i].Distance + Checks[i].Radius);
            return radius;
        }

        private static Vector3 ClosestPointSafe(Collider col, Vector3 point)
        {
            if (col is MeshCollider { convex: false })
                return col.ClosestPointOnBounds(point);
            return col.ClosestPoint(point);
        }

        private static void ResolveBarGeometry(Collider col, Vector3 fromPoint, out Vector3 grabPoint, out Vector3 axis)
        {
            Vector3 pointOnAxis;

            switch (col)
            {
                case CapsuleCollider capsule:
                    Vector3 capLocalDir = capsule.direction switch
                    {
                        0 => Vector3.right,
                        1 => Vector3.up,
                        _ => Vector3.forward,
                    };
                    axis = capsule.transform.TransformDirection(capLocalDir).normalized;
                    pointOnAxis = capsule.transform.TransformPoint(capsule.center);
                    break;

                case BoxCollider box:
                    Vector3 s = box.size;
                    Vector3 boxLocalDir =
                        (s.x >= s.y && s.x >= s.z) ? Vector3.right :
                        (s.y >= s.z) ? Vector3.up : Vector3.forward;
                    axis = box.transform.TransformDirection(boxLocalDir).normalized;
                    pointOnAxis = box.transform.TransformPoint(box.center);
                    break;

                default:
                    Bounds b = col.bounds;
                    Vector3 e = b.extents;
                    axis =
                        (e.x >= e.y && e.x >= e.z) ? Vector3.right :
                        (e.y >= e.z) ? Vector3.up : Vector3.forward;
                    pointOnAxis = b.center;
                    break;
            }

            Vector3 toPlayer = fromPoint - pointOnAxis;
            float t = Vector3.Dot(toPlayer, axis);
            grabPoint = pointOnAxis + axis * t;
        }

        #endregion

        #region Gizmos
#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            if (!DoDebug) return;

            Vector3 origin = RayOrigin;

            // Search range (only meaningful while a state has a check registered).
            if (HasChecks)
            {
                Gizmos.color = Hit.IsHit ? Color.green : Color.red;
                Gizmos.DrawWireSphere(origin, SearchRadius());
            }

            if (!Hit.IsHit) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(Hit.GrabPoint, 0.06f);
            Gizmos.DrawLine(origin, Hit.GrabPoint);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(Hit.GrabPoint, Hit.Axis * 0.75f);
            Gizmos.DrawRay(Hit.GrabPoint, -Hit.Axis * 0.75f);
        }

#endif
        #endregion
    }
}
