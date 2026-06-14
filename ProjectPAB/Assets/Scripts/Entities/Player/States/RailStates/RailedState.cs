using Entities.Player.Detection;
using Entities.Player.States.Base;
using Systems.Input;
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

        public RailedState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Railed;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            Ctx.GroundDetector.AddCheck(GroundCheck, Vector3.down, 0.8f, 0, CastType.SphereCast, radius: 0.5f);
            Ctx.RailDetector.AddCheck(RailCheck, Vector3.down, 1f, 0, CastType.SphereCast, radius: 0.35f);

            Ctx.Rigidbody.useGravity = false;
            Ctx.Rigidbody.isKinematic = true;

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
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");

            Ctx.GroundDetector.RemoveCheck(GroundCheck);
            Ctx.RailDetector.RemoveCheck(RailCheck);
            Ctx.Rigidbody.useGravity = true;
            Ctx.Rigidbody.isKinematic = false;
            _currentRail = null;
        }

        #region MonoBehaveiours

        public override void UpdateState()
        {
            CheckSwitchState();
        }

        public override void FixedUpdateState()
        {
            if (_currentRail == null) return;

            HandleRailGrind();
        }

        public override void LateUpdateState() { }

        #endregion

        private void HandleRailGrind()
        {
            float step = (Ctx.PlayerContext.GrindSpeed / _splineLength) * Time.fixedDeltaTime;
            _splineProgress += step * _grindDirection;

            if (_grindDirection == 1 && _splineProgress >= 1f)
            {
                _splineProgress = 1f;
                DetachFromRail();
                return;
            }
            if (_grindDirection == -1 && _splineProgress <= 0f)
            {
                _splineProgress = 0f;
                DetachFromRail();
                return;
            }

            NativeSpline worldSpline = new(_currentRail.Spline, _currentRail.transform.localToWorldMatrix);
            SplineUtility.Evaluate(worldSpline, _splineProgress, out float3 pos, out float3 forward, out float3 up);

            Vector3 targetPosition = (Vector3)pos + (Vector3.up);

            Ctx.Rigidbody.MovePosition(targetPosition);

            Vector3 lookDir = _grindDirection == 1 ? (Vector3)forward : -(Vector3)forward;

            lookDir = new(lookDir.x, 0f, lookDir.z);
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
            Ctx.PlayerObject.rotation = Quaternion.Slerp(Ctx.PlayerObject.rotation, targetRot, 15f * Time.fixedDeltaTime);
        }

        private void SnapToRail()
        {
            _splineLength = _currentRail.Container.CalculateLength();

            NativeSpline worldSpline = new NativeSpline(_currentRail.Spline, _currentRail.transform.localToWorldMatrix);
            SplineUtility.GetNearestPoint(worldSpline, Ctx.transform.position, out float3 worldNearestPos, out float t);
            _splineProgress = t;

            SplineUtility.Evaluate(worldSpline, t, out float3 worldPos, out float3 worldForward, out float3 worldUp);

            // Sample a second point slightly ahead to get a reliable direction
            float tAhead = Mathf.Clamp(t + 0.01f, 0f, 1f);
            SplineUtility.Evaluate(worldSpline, tAhead, out float3 posAhead, out _, out _);
            Vector3 derivedForward = ((Vector3)posAhead - (Vector3)worldPos).normalized;

            float dot = Vector3.Dot(Ctx.PlayerObject.forward, derivedForward);
            _grindDirection = dot >= 0 ? 1 : -1;

            Vector3 snappedPosition = (Vector3)worldPos + (Vector3.up);
            Ctx.transform.position = snappedPosition;
        }

        private void DetachFromRail()
        {
            NativeSpline worldSpline = new NativeSpline(_currentRail.Spline, _currentRail.transform.localToWorldMatrix);

            float tExit = _grindDirection == 1 ? 1f : 0f;
            float tBehind = _grindDirection == 1 ? 0.95f : 0.05f;

            SplineUtility.Evaluate(worldSpline, tExit, out float3 exitPos, out _, out _);
            SplineUtility.Evaluate(worldSpline, tBehind, out float3 behindPos, out _, out _);

            Vector3 launchDir = ((Vector3)exitPos - (Vector3)behindPos).normalized;

            launchDir = new Vector3(launchDir.x, 0f, launchDir.z).normalized;
            Vector3 launchVelocity = (launchDir * Ctx.PlayerContext.GrindSpeed) + (Vector3.up * 2f);

            _currentRail = null;
            Ctx.Rigidbody.isKinematic = false;
            Ctx.Rigidbody.linearVelocity = launchVelocity;
        }

        #region InputHandling

        protected override void HandleJumpInput(IReadOnlyButtonState jumpState)
        {
            if (Factory.HasState(PlayerStates.Jumping))
            {
                if (jumpState.UseBufferedPressOrHold())
                {
                    Vector3 railForward = Ctx.PlayerObject.forward;
                    railForward.y = 0f;
                    railForward.Normalize();

                    Vector3 railRight = Ctx.PlayerObject.right;
                    railRight.y = 0f;
                    railRight.Normalize();

                    Vector3 forwardMomentum = railForward;

                    float sidewaysInput = _moveDir.x;

                    float sidewaysJumpForce = 0.5f;
                    Vector3 sidewaysMomentum = railRight * sidewaysInput * sidewaysJumpForce;

                    Vector3 combinedHorizontal = forwardMomentum + sidewaysMomentum;

                    float jumpUpwardForce = 1f;
                    Vector3 finalDirection = new(combinedHorizontal.x, jumpUpwardForce, combinedHorizontal.z);

                    Ctx.JumpDirection = finalDirection;

                    TrySwitchState(PlayerStates.Jumping);
                }
            }
        }

        private Vector3 _moveDir;

        protected override void HandleMoveInput(IReadOnlyMovementInputState movementState)
        {
            _moveDir = movementState.RawInputValue;
        }

        #endregion

        #region StateLogic

        public override void InitializeSubState() { }

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Grounded))
            {
                if (Ctx.GroundDetector.HasAnyHit() && (!Ctx.RailDetector.HasAnyHit() || _currentRail == null))
                {
                    Debug.Log("switch to grounded");
                    TrySwitchState(PlayerStates.Grounded);
                }
            }

            if (Factory.HasState(PlayerStates.Falling))
            {
                if (_currentRail == null && !Ctx.GroundDetector.HasAnyHit() || !Ctx.RailDetector.HasAnyHit() && !Ctx.GroundDetector.HasAnyHit())
                {
                    Debug.Log("switch to falling");
                    TrySwitchState(PlayerStates.Falling);
                }
            }
        }

        #endregion
    }
}