using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class LedgeHangState : MovementBaseState
    {
        [Header("Ledge Detection Settings")]
        private const float LedgeCheckHeightOffset = 0.5f;
        private const float LedgeCheckForwardOffset = 0.2f;
        private const float LedgeCheckDistance = 0.7f;

        private Vector3 _ledgePoint;

        public LedgeHangState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.LedgeHanging;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif
        }

        public override void ExitState(PlayerBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif
        }

        #region MonoBehaviours

        public override void FixedUpdateState()
        {
            HandleClimbing();

            Quaternion faceWallRotation = Quaternion.LookRotation(-Ctx.WallDetector.WallNormal, Vector3.up);
            Ctx.SmoothModelRotation = Quaternion.Slerp(Ctx.PlayerModel.rotation, faceWallRotation, Time.fixedDeltaTime * 15f);
        }

        #endregion

        private void HandleClimbing()
        {
            Vector3 wallNormal = Ctx.WallDetector.WallNormal;
            Vector3 wallSideDir = Vector3.Cross(wallNormal, Vector3.up).normalized;
            Vector3 wallUpDir = Vector3.Cross(wallSideDir, wallNormal).normalized;

            Ctx.MoveDirection = (wallUpDir * 0) + (wallSideDir * _climbInput.x);

            Vector3 targetVelocity = Ctx.MoveDirection * Ctx.PlayerContext.ClimbSpeed;

            Ctx.Rigidbody.linearVelocity = Vector3.Lerp(Ctx.Rigidbody.linearVelocity, targetVelocity, Time.fixedDeltaTime * 15f);
        }

        /// <summary>
        /// Performs a localized downward trace relative to the current wall contact point.
        /// </summary>
        private bool EvaluateLedgeDetection()
        {
            if (Ctx.WallDetector.WallNormal == Vector3.zero) return false;

            Vector3 wallLookDirection = -Ctx.WallDetector.WallNormal;

            Vector3 downRayStart = Ctx.WallDetector.WallHit.point + (Vector3.up * LedgeCheckHeightOffset) + (wallLookDirection * LedgeCheckForwardOffset);

            Ray downRay = new(downRayStart, Vector3.down);

            LayerMask wallLayer = Ctx.WallDetector.WallHit.collider.gameObject.layer;

            if (Physics.Raycast(downRay, out RaycastHit ledgeHit, LedgeCheckDistance, 1 << wallLayer))
            {
                if (ledgeHit.normal.y > 0.7f)
                {
                    _ledgePoint = ledgeHit.point;
                    return true;
                }
            }

            return false;
        }

        #region Inputs

        private Vector2 _climbInput;

        protected override void HandleInputAction(IInputProvider input)
        {
            _climbInput = input.MovementState.RawInputValue;

            if (Factory.HasState(PlayerStates.WallClinging))
            {
                if (input.ShiftState.OnPressed())
                {
                    if (TrySwitchState(PlayerStates.WallClinging))
                        return;
                }
            }

            if (Factory.HasState(PlayerStates.ClimbUp))
            {
                if (input.JumpState.OnPressed() && _climbInput.y > 0)
                {
                    if (CanStandOnLedge())
                    {
                        if (TrySwitchState(PlayerStates.ClimbUp))
                        {
                            Ctx.GroundDetector.RegisterJumpTime();
                            return;
                        }
                    }
                }
            }

            if (Factory.HasState(PlayerStates.WallLunging))
            {
                if (Ctx.Stamina > 0)
                {
                    if (input.JumpState.UseBufferedPress() && _climbInput.y < 0)
                    {
                        Vector3 wallNormal = Ctx.WallDetector.WallNormal;
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
        }

        #endregion

        private const float StandableCheckDistance = 0.5f;

        private bool CanStandOnLedge()
        {
            Vector3 forwardFromLedge = -Ctx.WallDetector.WallNormal;
            float playerHeight = 2f;
            float playerRadius = 0.5f;
            Vector3 floorPoint = _ledgePoint + (forwardFromLedge * 0.2f);

            if (!Physics.Raycast(floorPoint + (Vector3.up * 0.1f), Vector3.down, out RaycastHit groundHit, 0.5f))
            {
                return false;
            }

            if (Physics.Raycast(groundHit.point, Vector3.up, out RaycastHit ceilingHit1, playerHeight))
            {
                return false;
            }

            if (Physics.SphereCast(groundHit.point, playerRadius, Vector3.up, out RaycastHit ceilingHit2, playerHeight))
            {
                return false;
            }

            if (groundHit.normal.y < 0.9f)
            {
                return false;
            }

            return true;
        }

        #region State Logic

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Climbing))
            {
                if (!EvaluateLedgeDetection())
                {
                    if (TrySwitchState(PlayerStates.Climbing))
                        return;
                }
            }
        }

        #endregion
    }
}