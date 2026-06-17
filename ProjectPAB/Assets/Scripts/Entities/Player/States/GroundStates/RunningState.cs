using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class RunningState : MovementBaseState
    {
        public RunningState(PlayerStateMachine currentContext, PlayerStateFactory charachterStateFactory) : base(currentContext, charachterStateFactory)
        {
            StateKey = PlayerStates.Running;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
        }

        #region MonoBehaveiours

        public override void FixedUpdateState()
        {
            HandleRunning();
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

        private void HandleRunning()
        {
            Vector3 moveDir = Ctx.MoveDirection;

            Vector3 targetVelocity = moveDir * Ctx.PlayerContext.RunSpeed;
            float accel = 20f;
            Ctx.Rigidbody.linearVelocity = Vector3.Lerp(Ctx.Rigidbody.linearVelocity, targetVelocity, Time.fixedDeltaTime * accel);

            if (moveDir.magnitude > 0.1f)
            {
                Vector3 rotationDir = new Vector3(moveDir.x, 0, moveDir.z);
                Quaternion targetRotation = Quaternion.LookRotation(rotationDir, Vector3.up);

                Ctx.PlayerObject.rotation = Quaternion.Slerp(Ctx.PlayerObject.rotation, targetRotation, Time.fixedDeltaTime * 10f);
            }
        }


        #endregion

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Walking))
            {
                if (!Ctx.IsRunInput)
                {
                    if (TrySwitchState(PlayerStates.Walking))
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