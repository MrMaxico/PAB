using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class WallClingingState : MovementBaseState
    {
        public WallClingingState(PlayerStateMachine currentContext, PlayerStateFactory charachterStateFactory) : base(currentContext, charachterStateFactory)
        {
            StateKey = PlayerStates.WallClinging;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.JumpDirection = Ctx.WallDetector.WallNormal + Vector3.up;

            Ctx.Rigidbody.linearVelocity = Vector3.zero;

            if (Ctx.Stamina > 0)
                Ctx.Rigidbody.useGravity = false;
            else
                Ctx.Rigidbody.useGravity = true;

            Quaternion faceFromWall = Quaternion.LookRotation(Ctx.WallDetector.WallNormal, Vector3.up);
            Ctx.SnapModelRotation = Quaternion.Euler(faceFromWall.eulerAngles.x, faceFromWall.eulerAngles.y, 0);
        }

        public override void ExitState(PlayerBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif
        }

        #region MonoBehaveiours

        public override void UpdateState()
        {
            if (Ctx.Stamina > 0)
            {
                Ctx.Stamina -= Time.deltaTime * 2f;
            }
            else
            {
                Ctx.Rigidbody.useGravity = true;

                Quaternion faceFromWall = Quaternion.LookRotation(Ctx.WallDetector.WallNormal, Vector3.up);
                Ctx.SnapModelRotation = Quaternion.Euler(faceFromWall.eulerAngles.x, faceFromWall.eulerAngles.y, 0);
            }
        }

        #endregion

        #region Inputs

        protected override void HandleInputAction(IInputProvider input)
        {
            if (Factory.HasState(PlayerStates.Climbing))
            {
                if (Ctx.Stamina > 0)
                {
                    if (input.ShiftState.OnReleased())
                    {
                        if (TrySwitchState(PlayerStates.Climbing))
                            return;
                    }
                }
            }

            if (Factory.HasState(PlayerStates.Jumping))
            {
                if (input.JumpState.UseBufferedPress())
                {
                    if (TrySwitchRootState(PlayerStates.Jumping))
                        return;
                }
            }
        }

        #endregion
    }
}