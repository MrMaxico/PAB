using Entities.Player.Detection;
using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class ClimbingState : MovementBaseState
    {
        [Header("Ledge Detection Settings")]
        private const float LedgeCheckHeightOffset = 0.5f; // How far above the wall hit to look down
        private const float LedgeCheckForwardOffset = 0.2f; // How far forward into the wall to look down
        private const float LedgeCheckDistance = 0.7f;      // Length of the downward ray

        private bool _hasLedge;
        private Vector3 _ledgePoint;

        private const string MoveCheck = "Move";

        public ClimbingState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Climbing;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.Rigidbody.useGravity = false;
            Ctx.Rigidbody.linearVelocity = Vector3.zero;
            Ctx.JumpDirection = Vector3.up * 1.2f;

            Quaternion faceWallRotation = Quaternion.LookRotation(-Ctx.WallDetector.WallNormal, Vector3.up);
            Ctx.RotationSnapped = true;
            Ctx.SnapModelRotation = Quaternion.Euler(faceWallRotation.eulerAngles.x, faceWallRotation.eulerAngles.y, 0);

            Ctx.WallDetector.AddMovementCheck(MoveCheck, 0.9f, 0, CastType.SphereCast, radius: 0.3f);
        }

        public override void ExitState(PlayerBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.WallDetector.RemoveCheck(MoveCheck);
        }

        #region MonoBehaviours

        public override void UpdateState()
        {
            if (Ctx.Stamina > 0)
            {
                Ctx.Stamina -= Time.deltaTime * 10f;
            }

            // ─── Inline Ledge Detection ───

        }

        public override void FixedUpdateState()
        {
            HandleClimbing();
        }

        private void HandleClimbing()
        {
            Vector3 activeWallNormal = Ctx.WallDetector.WallNormal;
            Vector3 activeWallPoint = Ctx.WallDetector.WallHit.point;

            if (Ctx.MoveDirection != Vector3.zero && Ctx.WallDetector.TryGetHit("Move", out RaycastHit moveHit))
            {
                activeWallNormal = moveHit.normal;
                activeWallPoint = moveHit.point;
            }

            Vector3 wallSideDir = Vector3.Cross(activeWallNormal, Vector3.up).normalized;
            Vector3 wallUpDir = Vector3.Cross(wallSideDir, activeWallNormal).normalized;

            Ctx.MoveDirection = (wallUpDir * _climbInput.y) + (wallSideDir * _climbInput.x);
            Vector3 targetVelocity = Ctx.MoveDirection * Ctx.PlayerContext.ClimbSpeed;

            Ctx.Rigidbody.linearVelocity = Vector3.Lerp(Ctx.Rigidbody.linearVelocity, targetVelocity, Time.fixedDeltaTime * 15f);

            Quaternion faceWallRotation = Quaternion.LookRotation(-activeWallNormal, Vector3.up);
            Ctx.SmoothModelRotation = Quaternion.Slerp(Ctx.SmoothModelRotation, faceWallRotation, Time.fixedDeltaTime * 15f);
        }

        #endregion

        #region Ledge Calculation

        /// <summary>
        /// Performs a localized downward trace relative to the current wall contact point.
        /// </summary>
        private bool EvaluateLedgeDetection()
        {
            if (Ctx.WallDetector.WallNormal == Vector3.zero) return false;

            Vector3 wallLookDirection = -Ctx.WallDetector.WallNormal;

            Vector3 downRayStart = Ctx.WallDetector.WallHit.point
                                   + (Vector3.up * LedgeCheckHeightOffset)
                                   + (wallLookDirection * LedgeCheckForwardOffset);

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

        #region State Logic

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

        #endregion
    }
}