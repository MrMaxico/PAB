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

        [SerializeField] private bool _includeTriggers = true;

        [Header("Submersion")]
        [Tooltip("How far above the head to search for the water surface when fully submerged.")]
        [SerializeField] private float _surfaceProbeHeight = 10f;

        // --- Results ---
        public DetectionHit Hit { get; private set; }
        public bool HasHit => Hit.IsHit;

        // How much of the body's vertical extent is below the water surface, 0-1.
        // 0 = not touching water, 1 = fully submerged. NaN-safe: stays 0 if no surface is found.
        public float SubmersionFraction { get; private set; }
        public bool HasWaterSurface { get; private set; }
        public float WaterSurfaceHeight { get; private set; }

        private Vector3 _lastMovementDirection;

        private Transform DirSource => _playerObject != null ? _playerObject : transform;

        #region Registration

        public void AddRay(string id, Vector3 direction, float distance, int priority = 0)
        {
            var check = MovementCheck.Ray(id, direction, distance, priority);
            if (_includeTriggers) check.IncludeTriggers();
            AddCheck(check);
        }

        public void AddSphere(string id, Vector3 direction, float distance, float radius, int priority = 0)
        {
            var check = MovementCheck.Sphere(id, direction, distance, radius, priority);
            if (_includeTriggers) check.IncludeTriggers();
            AddCheck(check);
        }

        public void AddMovementRay(string id, float distance, int priority = 0)
        {
            var check = MovementCheck.Movement(id, distance, priority);
            if (_includeTriggers) check.IncludeTriggers();
            AddCheck(check);
        }

        public void AddMovementSphere(string id, float distance, float radius, int priority = 0)
        {
            var check = MovementCheck.Movement(id, distance, priority);
            check.AsSphere(radius);
            if (_includeTriggers) check.IncludeTriggers();
            AddCheck(check);
        }

        #endregion

        #region Tick Hooks

        public override void Tick(Vector3 movementDirection = default)
        {
            _lastMovementDirection = movementDirection.normalized;
            base.Tick(movementDirection);
        }

        protected override void ClearHit() => Hit = DetectionHit.None;

        protected override void OnHit(RaycastHit rawHit) => Hit = new DetectionHit(rawHit);

        protected override Vector3 ResolveCastDirection(MovementCheck check, Vector3 movementDirection) =>
            check.UseMovementDirection
                ? movementDirection
                : DirSource.TransformDirection(check.Direction);

        #endregion

        #region Submersion

        // Entry-direction independent: point-in-water checks tell us WHERE the body is relative
        // to the water, then a directional probe finds the surface. Works when entering from the
        // top, wading in from the side, being fully underwater, or rising into a volume from below.
        // (A plain downward ray fails underwater because raycasts never hit a collider from inside.)
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

            QueryTriggerInteraction triggers = _includeTriggers
                ? QueryTriggerInteraction.Collide
                : QueryTriggerInteraction.Ignore;

            float x = transform.position.x;
            float z = transform.position.z;

            bool bottomInWater = Physics.CheckSphere(new Vector3(x, bodyBottomY + 0.05f, z), 0.02f, _detectionLayer, triggers);
            bool topInWater = Physics.CheckSphere(new Vector3(x, bodyTopY - 0.05f, z), 0.02f, _detectionLayer, triggers);

            if (!bottomInWater && !topInWater)
            {
                HasWaterSurface = false;
                SubmersionFraction = 0f;
                return;
            }

            if (topInWater && !bottomInWater)
            {
                // Rising into a floating volume from below: find its underside with an up-cast
                // (starts below the volume, so it hits the bottom face).
                Vector3 underOrigin = new(x, bodyBottomY - 0.25f, z);
                if (Physics.Raycast(underOrigin, Vector3.up, out RaycastHit underHit, bodyHeight + 0.5f, _detectionLayer, triggers))
                {
                    HasWaterSurface = false; // no surface *below* the head to float on
                    SubmersionFraction = Mathf.Clamp01((bodyTopY - underHit.point.y) / bodyHeight);
                    return;
                }
            }

            if (topInWater)
            {
                // Head under water. Probe down from high above — the probe starts outside the
                // volume, so the first hit is the real surface.
                Vector3 probeOrigin = new(x, bodyTopY + _surfaceProbeHeight, z);
                if (Physics.Raycast(probeOrigin, Vector3.down, out RaycastHit surfaceHit, _surfaceProbeHeight + bodyHeight, _detectionLayer, triggers))
                {
                    HasWaterSurface = true;
                    WaterSurfaceHeight = surfaceHit.point.y;
                }
                else
                {
                    // Even the probe start is under water (very deep); surface height unknown.
                    HasWaterSurface = false;
                    WaterSurfaceHeight = bodyTopY;
                }

                SubmersionFraction = 1f;
                return;
            }

            // Bottom in water, head above it: the surface sits between them, so a short ray from
            // just above the head starts outside the volume and finds it — regardless of whether
            // the water was entered from the top or waded into from the side.
            Vector3 castOrigin = new(x, bodyTopY + 0.25f, z);
            if (Physics.Raycast(castOrigin, Vector3.down, out RaycastHit hit, bodyHeight + 0.5f, _detectionLayer, triggers))
            {
                HasWaterSurface = true;
                WaterSurfaceHeight = hit.point.y;
                SubmersionFraction = Mathf.Clamp01((hit.point.y - bodyBottomY) / bodyHeight);
            }
            else
            {
                // Feet are wet but no top face above us (unusual volume shape); best guess.
                HasWaterSurface = false;
                SubmersionFraction = 0.5f;
            }
        }

        #endregion

        #region Gizmos
#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            if (!DoDebug) return;

            Vector3 origin = RayOrigin;

            for (int i = 0; i < Checks.Count; i++)
            {
                MovementCheck check = Checks[i];
                Vector3 worldDir = check.UseMovementDirection
                    ? (_lastMovementDirection != Vector3.zero ? _lastMovementDirection : DirSource.forward)
                    : DirSource.TransformDirection(check.Direction);

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
        #endregion
    }
}
