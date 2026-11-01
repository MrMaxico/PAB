using Entities.Base.Detection;
using Entities.Base.States.Base;
using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class ClimbingState : PlayerMovementState
    {
        private const float LedgeCheckHeightOffset = 0.5f; // How far above the wall hit to look down
        private const float LedgeCheckForwardOffset = 0.2f; // How far forward into the wall to look down
        private const float LedgeCheckDistance = 0.7f;      // Length of the downward ray

        private Vector3 _ledgePoint;

        private readonly Probe _move;

        public ClimbingState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Climbing;

            _move = Probe.Sphere(currentContext.Rigidbody, Vector3.zero, 0.9f, 0.3f)
                .On(currentContext.PlayerContext.WallLayer)
                .Offset(Vector3.up * currentContext.PlayerContext.WallProbeHeight)
                .Along(() => currentContext.MoveDirection)
                .Gated(currentContext.WallGate);
        }

        public override void EnterState(EntityBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.Rigidbody.useGravity = false;
            Ctx.Rigidbody.linearVelocity = Vector3.zero;
            Ctx.JumpDirection = Vector3.up * 1.2f;

            Quaternion faceWallRotation = Quaternion.LookRotation(-Ctx.Walls.Hit.normal, Vector3.up);
            Ctx.SnapModelRotation = Quaternion.Euler(faceWallRotation.eulerAngles.x, faceWallRotation.eulerAngles.y, 0);
        }

        public override void ExitState(EntityBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif
        }

        #region MonoBehaviours

        public override void UpdateState()
        {
            if (Ctx.Stamina > 0)
            {
                Ctx.Stamina -= Time.deltaTime * 10f;
            }

        }

        public override void FixedUpdateState()
        {
            HandleClimbing();
        }

        #endregion

        #region State Logic

        private void HandleClimbing()
        {
            Vector3 activeWallNormal = Ctx.Walls.Hit.normal;
            Vector3 activeWallPoint = Ctx.Walls.Hit.point;

            if (Ctx.MoveDirection != Vector3.zero && _move.TryGetHit(out RaycastHit moveHit))
            {
                activeWallNormal = moveHit.normal;
                activeWallPoint = moveHit.point;
            }

            Vector3 wallSideDir = Vector3.Cross(activeWallNormal, Vector3.up).normalized;
            Vector3 wallUpDir = Vector3.Cross(wallSideDir, activeWallNormal).normalized;

            Ctx.MoveDirection = (wallUpDir * _climbInput.y) + (wallSideDir * _climbInput.x);

            Vector3 currentVel = Ctx.Rigidbody.linearVelocity;
            Vector3 targetVelocity = Ctx.MoveDirection * Ctx.PlayerContext.ClimbSpeed + Ctx.PlatformVelocity;

            Ctx.Rigidbody.linearVelocity = Vector3.Lerp(currentVel, targetVelocity, Time.fixedDeltaTime * 15f);

            Quaternion faceWallRotation = Quaternion.LookRotation(-activeWallNormal, Vector3.up);
            Ctx.SmoothModelRotation = Quaternion.Slerp(Ctx.SmoothModelRotation, faceWallRotation, Time.fixedDeltaTime * 15f);
        }

        private bool EvaluateLedgeDetection()
        {
            if (Ctx.Walls.Hit.normal == Vector3.zero)
                return false;

            Vector3 wallLookDirection = -Ctx.Walls.Hit.normal;

            Vector3 downRayStart = Ctx.Walls.Hit.point
                                   + (Vector3.up * LedgeCheckHeightOffset)
                                   + (wallLookDirection * LedgeCheckForwardOffset);

            Ray downRay = new(downRayStart, Vector3.down);

            LayerMask wallLayer = Ctx.Walls.Hit.LayerMaskOf();

            if (Physics.Raycast(downRay, out RaycastHit ledgeHit, LedgeCheckDistance, wallLayer))
            {
                if (ledgeHit.normal.y > 0.7f)
                {
                    _ledgePoint = ledgeHit.point;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Inputs

        private Vector2 _climbInput;

        protected override void HandleInputAction(IInputProvider input)
        {
            _climbInput = input.MovementState.RawInputValue;

            if (Factory.HasState(PlayerStates.WallLunging))
            {
                if (Ctx.Stamina > 0)
                {
                    if (input.JumpState.UseBufferedPress())
                    {
                        Vector3 wallNormal = Ctx.Walls.Hit.normal;
                        Vector3 wallSideDir = Vector3.Cross(wallNormal, Vector3.up);
                        Vector3 wallUpDir = Vector3.up;

                        Vector3 lungeDir = (wallUpDir * _climbInput.y) + (wallSideDir * _climbInput.x);

                        if (lungeDir.magnitude < 0.1f) lungeDir = Vector3.up;

                        Ctx.JumpDirection = lungeDir.normalized;

                        if (TrySwitchState(PlayerStates.WallLunging))
                            return;
                    }
                }
            }

            if (input.ShiftState.OnPressed())
            {
                if (Factory.HasState(PlayerStates.WallClinging))
                {
                    if (TrySwitchState(PlayerStates.WallClinging))
                        return;
                }
            }
        }

        #endregion

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.WallClinging))
            {
                if (Ctx.Stamina <= 0)
                {
                    if (TrySwitchState(PlayerStates.WallClinging))
                        return;
                }
            }

            if (Factory.HasState(PlayerStates.LedgeHanging))
            {
                if (EvaluateLedgeDetection())
                {
                    if (TrySwitchState(PlayerStates.LedgeHanging))
                        return;
                }
            }
        }
    }
}