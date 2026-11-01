using Entities.Base.States;
using Entities.Base.States.Base;
using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class RunningState : PlayerMovementState
    {
        public RunningState(PlayerStateMachine currentContext, PlayerStateFactory charachterStateFactory) : base(currentContext, charachterStateFactory)
        {
            StateKey = PlayerStates.Running;
        }

        public override void EnterState(EntityBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif
        }

        public override void ExitState(EntityBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif
        }

        #region MonoBehaveiours

        public override void FixedUpdateState()
        {
            HandleRunning();
        }

        #endregion

        #region State Logic

        private void HandleRunning()
        {
            Vector3 moveDir = Ctx.MoveDirection;

            Vector3 currentVel = Ctx.Rigidbody.linearVelocity;
            Vector3 targetVelocity = moveDir * Ctx.PlayerContext.RunSpeed + Ctx.PlatformVelocity;
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