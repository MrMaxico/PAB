using UnityEngine;

namespace Entities.Player.Detection
{
    public class WallDetector : BaseDetector<MovementCheck>
    {
        [SerializeField] private bool DoDebug;

        [Header("References")]
        [SerializeField] private Transform _playerObject;

        [Header("Settings")]
        [SerializeField] private LayerMask _wallLayer;
        [SerializeField] private float _wallJumpGracePeriod = 0.1f;
        [SerializeField] private float _originHeight = 0.5f;

        // --- Results ---
        public DetectionHit Hit { get; private set; }

        protected override LayerMask DefaultLayerMask => _wallLayer;

        private float _lastWallJumpTime;
        private Vector3 RayOrigin => transform.position + Vector3.up * _originHeight;

        // ─── Registration shorthands ───

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

        // ─── Tick ───

        public void Tick(Vector3 movementDirection = new())
        {
            if (Time.time - _lastWallJumpTime < _wallJumpGracePeriod)
            {
                ResetHits();
                Hit = DetectionHit.None;
                return;
            }

            Vector3 normalizedMovement = movementDirection.normalized;
            CastChecks(RayOrigin, check =>
                check.UseMovementDirection
                    ? normalizedMovement
                    : _playerObject.TransformDirection(check.Direction));

            if (TryGetBestHit(out RaycastHit rawHit))
            {
                Vector3 wallNormal = rawHit.normal;
                Vector3 velocityOnWall = Vector3.ProjectOnPlane(GetComponent<Rigidbody>().linearVelocity, wallNormal);
                Vector3 wallForward = velocityOnWall.sqrMagnitude > 0.01f
                    ? velocityOnWall.normalized
                    : Vector3.ProjectOnPlane(_playerObject.forward, wallNormal).normalized;

                Hit = new DetectionHit(rawHit, forward: wallForward);
            }
            else
            {
                Hit = DetectionHit.None;
                ResetHits();
            }
        }

        public void RegisterJumpTime() => _lastWallJumpTime = Time.time;

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
            Vector3 stateMachineMoveDir = Vector3.zero;

            if (Application.isPlaying && TryGetComponent(out PlayerStateMachine psm))
                stateMachineMoveDir = psm.MoveDirection.normalized;

            for (int i = 0; i < Checks.Count; i++)
            {
                MovementCheck check = Checks[i];
                Vector3 worldDir;

                if (check.UseMovementDirection)
                {
                    worldDir = stateMachineMoveDir != Vector3.zero ? stateMachineMoveDir : _playerObject.forward;
                    Gizmos.color = Color.pink;
                }
                else
                {
                    worldDir = _playerObject.TransformDirection(check.Direction);
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
    }
}