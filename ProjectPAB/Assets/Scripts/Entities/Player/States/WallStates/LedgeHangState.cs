using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class LedgeHangState : MovementBaseState
    {
        public LedgeHangState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.LedgeHanging;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
        }

        #region MonoBehaviours

        public override void FixedUpdateState()
        {
            HandleClimbing();

            Quaternion faceWallRotation = Quaternion.LookRotation(-Ctx.WallDetector.WallNormal, Vector3.up);
            Ctx.PlayerObject.rotation = Quaternion.Slerp(Ctx.PlayerObject.rotation, faceWallRotation, Time.fixedDeltaTime * 15f);
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
                    // add check to see if there is standable ground above ledge
                    if (TrySwitchState(PlayerStates.ClimbUp))
                        return;
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
    }
}