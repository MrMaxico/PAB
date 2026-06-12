using Entities.Player.Detection;
using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class ClimbingState : MovementBaseState
    {
        private const string FrontExtra = "FrontExtra";

        public ClimbingState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Climbing;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            Ctx.WallDetector.AddCheck(FrontExtra, Vector3.forward, 0.9f, 4, CastType.Raycast);

            Ctx.Rigidbody.useGravity = false;
            Ctx.Rigidbody.linearVelocity = Vector3.zero;
            Ctx.JumpDirection = Vector3.up * 1.2f;

            Quaternion faceWallRotation = Quaternion.LookRotation(-Ctx.WallDetector.WallNormal, Vector3.up);
            Ctx.PlayerObject.rotation = Quaternion.Euler(faceWallRotation.eulerAngles.x, faceWallRotation.eulerAngles.y, 0);

            Ctx.WallDetector.Tick();
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");

            Ctx.WallDetector.RemoveCheck(FrontExtra);
        }

        #region MonoBehaviours

        public override void UpdateState()
        {
            if (Ctx.Stamina > 0)
            {
                Ctx.Stamina -= Time.deltaTime * 10f;
            }
            else if (Factory.HasState(PlayerStates.WallClinging))
            {
                TrySwitchState(PlayerStates.WallClinging);
            }

            if (Factory.HasState(PlayerStates.ClimbUp))
            {
                if (!Ctx.WallDetector.IsHit(FrontExtra))
                {
                    TrySwitchState(PlayerStates.ClimbUp);
                }
            }
        }

        public override void FixedUpdateState()
        {
            HandleClimbing();

            Quaternion faceWallRotation = Quaternion.LookRotation(-Ctx.WallDetector.WallNormal, Vector3.up);
            Ctx.PlayerObject.rotation = Quaternion.Slerp(Ctx.PlayerObject.rotation, faceWallRotation, Time.fixedDeltaTime * 15f);
        }

        private void HandleClimbing()
        {
            Vector3 wallNormal = Ctx.WallDetector.WallNormal;
            Vector3 wallSideDir = Vector3.Cross(wallNormal, Vector3.up).normalized;
            Vector3 wallUpDir = Vector3.Cross(wallSideDir, wallNormal).normalized;

            Ctx.MoveDirection = (wallUpDir * _climbInput.y) + (wallSideDir * _climbInput.x);

            Vector3 targetVelocity = Ctx.MoveDirection * Ctx.PlayerContext.ClimbSpeed;

            Ctx.Rigidbody.linearVelocity = Vector3.Lerp(Ctx.Rigidbody.linearVelocity, targetVelocity, Time.fixedDeltaTime * 15f);
        }

        public override void LateUpdateState() { }

        #endregion

        #region Inputs

        private Vector2 _climbInput;

        protected override void HandleMoveInput(IReadOnlyMovementInputState movementState)
        {
            _climbInput = movementState.RawInputValue;
        }

        protected override void HandleJumpInput(IReadOnlyButtonState jumpingState)
        {
            if (Factory.HasState(PlayerStates.WallLunging))
            {
                if (Ctx.Stamina > 0)
                {
                    if (jumpingState.UseBufferedPress())
                    {
                        Vector3 wallNormal = Ctx.WallDetector.WallNormal;
                        Vector3 wallSideDir = Vector3.Cross(wallNormal, Vector3.up);
                        Vector3 wallUpDir = Vector3.up;

                        Vector3 lungeDir = (wallUpDir * _climbInput.y) + (wallSideDir * _climbInput.x);

                        if (lungeDir.magnitude < 0.1f) lungeDir = Vector3.up;

                        Ctx.JumpDirection = lungeDir.normalized;

                        TrySwitchState(PlayerStates.WallLunging);
                    }
                }
            }
        }

        protected override void HandleShiftInput(IReadOnlyButtonState shiftingState)
        {
            if (shiftingState.WasPressedThisFrame)
            {
                if (Factory.HasState(PlayerStates.WallClinging))
                {
                    TrySwitchState(PlayerStates.WallClinging);
                }
            }
        }

        #endregion

        #region State Logic

        public override void CheckSwitchState()
        {

        }

        #endregion
    }
}
