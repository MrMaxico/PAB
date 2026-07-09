using UnityEngine;
using UnityEngine.Serialization;

namespace Entities.Player.Detection
{
    public class WallDetector : BaseDetector<MovementCheck>
    {
        [SerializeField] private bool DoDebug;

        [Header("References")]
        [SerializeField] private Transform _playerObject;

        [Header("Settings")]
        [FormerlySerializedAs("_wallLayer")]
        [SerializeField] private LayerMask _detectionLayer;
        public LayerMask DetectionLayer => _detectionLayer;
        protected override LayerMask DefaultLayerMask => _detectionLayer;

        // --- Results ---
        public DetectionHit Hit { get; private set; }
        public bool HasHit => Hit.IsHit;

        private Rigidbody _rigidbody;

        private Transform DirSource => _playerObject != null ? _playerObject : transform;

        #region Registration

        public void AddRay(string id, Vector3 direction, float distance, int priority = 0) =>
            AddCheck(MovementCheck.Ray(id, direction, distance, priority));

        public void AddSphere(string id, Vector3 direction, float distance, float radius, int priority = 0) =>
            AddCheck(MovementCheck.Sphere(id, direction, distance, radius, priority));

        public void AddMovementRay(string id, float distance, int priority = 0) =>
            AddCheck(MovementCheck.Movement(id, distance, priority));

        public void AddMovementSphere(string id, float distance, float radius, int priority = 0)
        {
            var check = MovementCheck.Movement(id, distance, priority);
            check.AsSphere(radius);
            AddCheck(check);
        }

        #endregion

        #region Tick Hooks

        protected override void ClearHit() => Hit = DetectionHit.None;

        protected override void OnHit(RaycastHit rawHit)
        {
            _rigidbody ??= GetComponent<Rigidbody>();

            Vector3 wallNormal = rawHit.normal;
            Vector3 velocityOnWall = Vector3.ProjectOnPlane(_rigidbody.linearVelocity, wallNormal);
            Vector3 wallForward = velocityOnWall.sqrMagnitude > 0.01f
                ? velocityOnWall.normalized
                : Vector3.ProjectOnPlane(DirSource.forward, wallNormal).normalized;

            Hit = new DetectionHit(rawHit, forward: wallForward);
        }

        protected override Vector3 ResolveCastDirection(MovementCheck check, Vector3 movementDirection) =>
            check.UseMovementDirection
                ? movementDirection
                : DirSource.TransformDirection(check.Direction);

        #endregion

        #region Gizmos
#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            if (!DoDebug) return;

            Vector3 origin = RayOrigin;
            Vector3 stateMachineMoveDir = Vector3.zero;

            if (Application.isPlaying && TryGetComponent(out PlayerStateMachine psm))
                stateMachineMoveDir = psm.MoveDirection.normalized;

            for (int i = 0; i < Checks.Count; i++)
            {
                MovementCheck check = Checks[i];
                Vector3 worldDir;

                if (check.UseMovementDirection)
                {
                    worldDir = stateMachineMoveDir != Vector3.zero ? stateMachineMoveDir : DirSource.forward;
                    Gizmos.color = Color.pink;
                }
                else
                {
                    worldDir = DirSource.TransformDirection(check.Direction);
                    Gizmos.color = check.IsHit ? Color.green : Color.red;
                }

                Gizmos.DrawRay(origin, worldDir * check.Distance);

                if (check.CastType == CastType.SphereCast)
                    Gizmos.DrawWireSphere(origin + worldDir * check.Distance, check.Radius);
            }

            if (!Hit.IsHit) return;

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(Hit.Point, 0.05f);
            Gizmos.DrawRay(Hit.Point, Hit.Normal * 0.5f);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(origin, Hit.Forward * 1.0f);
        }

#endif
        #endregion
    }
}
