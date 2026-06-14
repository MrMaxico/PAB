using Entities.Player.Detection;
using Entities.Player.States.Base;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Entities.Player.States
{
    public class RailedState : PlayerBaseState
    {
        private const string GroundCheck = "Ground";
        private const string RailCheck = "Rail";

        private GrindRail _currentRail;
        private float _splineProgress;
        private int _grindDirection = 1;
        private float _splineLength;

        private float _verticalOffset = 1.5f;

        private float _railExitCooldown = 0.2f;
        private float _cooldownTimer = 0f;

        public RailedState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Railed;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
            // Reset cooldown when entering
            _cooldownTimer = 0f;

            Ctx.GroundDetector.AddCheck(GroundCheck, Vector3.down, 0.8f, 0, CastType.SphereCast, radius: 0.5f);
            Ctx.RailDetector.AddCheck(RailCheck, Vector3.down, 0.8f, 0, CastType.SphereCast, radius: 0.35f);

            Ctx.Rigidbody.useGravity = false;
            Ctx.Rigidbody.isKinematic = true;

            Ctx.Rigidbody.linearVelocity = Vector3.zero;

            Ctx.GroundDetector.Tick();
            Ctx.RailDetector.Tick();

            if (Ctx.RailDetector.TryGetHit(RailCheck, out var hit))
            {
                if (hit.collider.TryGetComponent<GrindRail>(out var rail))
                {
                    _currentRail = rail;
                    SnapToRail();
                }
            }
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Ctx.GroundDetector.RemoveCheck(GroundCheck);
            Ctx.RailDetector.RemoveCheck(RailCheck);
            Ctx.Rigidbody.useGravity = true;
            Ctx.Rigidbody.isKinematic = false;
            _currentRail = null;
        }

        public override void UpdateState()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }

            CheckSwitchState();
        }

        public override void FixedUpdateState()
        {
            if (_currentRail == null) return;

            float step = (Ctx.PlayerContext.GrindSpeed / _splineLength) * Time.fixedDeltaTime;
            _splineProgress += step * _grindDirection;

            if (_splineProgress < 0f || _splineProgress > 1f)
            {
                DetachFromRail();
                return;
            }

            NativeSpline worldSpline = new NativeSpline(_currentRail.Spline, _currentRail.transform.localToWorldMatrix);
            SplineUtility.Evaluate(worldSpline, _splineProgress, out float3 pos, out float3 forward, out float3 up);

            Vector3 targetPosition = (Vector3)pos + (Vector3.up * _verticalOffset);

            Ctx.Rigidbody.MovePosition(targetPosition);

            Vector3 lookDir = _grindDirection == 1 ? (Vector3)forward : -(Vector3)forward;

            lookDir = new(lookDir.x, 0f, lookDir.z);
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
            Ctx.PlayerObject.rotation = Quaternion.Slerp(Ctx.PlayerObject.rotation, targetRot, 15f * Time.fixedDeltaTime);
        }

        public override void LateUpdateState() { }

        private void SnapToRail()
        {
            _splineLength = _currentRail.Container.CalculateLength();

            NativeSpline worldSpline = new NativeSpline(_currentRail.Spline, _currentRail.transform.localToWorldMatrix);
            SplineUtility.GetNearestPoint(worldSpline, Ctx.transform.position, out float3 worldNearestPos, out float t);
            _splineProgress = t;

            SplineUtility.Evaluate(worldSpline, t, out float3 worldPos, out float3 worldForward, out float3 worldUp);

            float dot = Vector3.Dot(Ctx.PlayerObject.forward, (Vector3)worldForward);
            _grindDirection = dot >= 0 ? 1 : -1;

            Vector3 snappedPosition = (Vector3)worldPos + (Vector3.up * _verticalOffset);
            Ctx.transform.position = snappedPosition;
        }

        private void DetachFromRail()
        {
            Vector3 launchDirection = _grindDirection == 1 ? _currentRail.transform.forward : -_currentRail.transform.forward;

            Vector3 launchVelocity = (launchDirection * Ctx.PlayerContext.GrindSpeed) + (Vector3.up * 2f);

            _currentRail = null;

            TrySwitchState(PlayerStates.Falling);

            Ctx.Rigidbody.linearVelocity = launchVelocity;
        }

        public override void InitializeSubState() { }

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Grounded))
            {
                if (Ctx.GroundDetector.HasAnyHit() && !Ctx.RailDetector.HasAnyHit())
                    TrySwitchState(PlayerStates.Grounded);
            }

            if (Factory.HasState(PlayerStates.Falling))
            {
                if (_currentRail == null)
                    TrySwitchState(PlayerStates.Falling);
            }
        }
    }
}