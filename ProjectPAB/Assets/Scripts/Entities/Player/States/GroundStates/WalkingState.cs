using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class WalkingState : MovementBaseState
    {
        public WalkingState(PlayerStateMachine currentContext, PlayerStateFactory charachterStateFactory) : base(currentContext, charachterStateFactory)
        {
            StateKey = PlayerStates.Walking;
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

        #region MonoBehaveiours

        public override void FixedUpdateState()
        {
            HandleWalking();

            if (Ctx.WallDetector.IsHit("Front"))
            {
                Vector3 playerMoveDir = Ctx.Rigidbody.linearVelocity.normalized;
                Vector3 wallNormal = Ctx.WallDetector.Hit.Normal;
                float dotProduct = Vector3.Dot(playerMoveDir, wallNormal);

                if (dotProduct < -0.5f)
                {
                    if (TrySwitchRootState(PlayerStates.Jumping))
                        return;
                }
            }
        }

        #endregion

        #region Inputs

        protected override void HandleInputAction(IInputProvider input)
        {
            if (Factory.HasState(PlayerStates.Sliding))
            {
                if (input.SlideState.OnPressed())
                {
                    if (TrySwitchState(PlayerStates.Sliding))
                        return;
                }
            }
        }

        #endregion

        #region State Logic

        private void HandleWalking()
        {
            Vector3 moveDir = Ctx.MoveDirection;

            Vector3 currentVel = Ctx.Rigidbody.linearVelocity;
            Vector3 targetVelocity = moveDir * Ctx.PlayerContext.WalkSpeed + Ctx.PlatformVelocity;
            targetVelocity.y = currentVel.y;
            float accel = 20f;
            Ctx.Rigidbody.linearVelocity = Vector3.Lerp(currentVel, targetVelocity, Time.fixedDeltaTime * accel);

            if (moveDir.magnitude > 0.1f)
            {
                Vector3 rotationDir = new(moveDir.x, 0, moveDir.z);
                Quaternion targetRotation = Quaternion.LookRotation(rotationDir, Vector3.up);

                Ctx.SmoothModelRotation = Quaternion.Slerp(Ctx.PlayerModel.rotation, targetRotation, Time.fixedDeltaTime * 10f);
            }
        }

        #endregion

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Running))
            {
                if (Ctx.IsMovementInput && Ctx.IsRunInput && Ctx.GroundDetector.HasAnyHit())
                {
                    if (TrySwitchState(PlayerStates.Running))
                        return;
                }
            }

            if (Factory.HasState(PlayerStates.Idling))
            {
                if (!Ctx.IsMovementInput)
                {
                    if (TrySwitchState(PlayerStates.Idling))
                        return;
                }
            }
        }
    }
}