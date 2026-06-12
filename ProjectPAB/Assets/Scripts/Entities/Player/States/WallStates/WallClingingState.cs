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
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            Ctx.JumpDirection = Ctx.WallDetector.WallNormal + Vector3.up;

            Ctx.Rigidbody.linearVelocity = Vector3.zero;

            if (Ctx.Stamina > 0)
                Ctx.Rigidbody.useGravity = false;
            else
                Ctx.Rigidbody.useGravity = true;

            Quaternion faceFromWall = Quaternion.LookRotation(Ctx.WallDetector.WallNormal, Vector3.up);
            Ctx.PlayerObject.rotation = Quaternion.Euler(faceFromWall.eulerAngles.x, faceFromWall.eulerAngles.y, 0);
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
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
                Ctx.PlayerObject.rotation = Quaternion.Euler(faceFromWall.eulerAngles.x, faceFromWall.eulerAngles.y, 0);
            }
        }

        public override void FixedUpdateState() { }

        public override void LateUpdateState() { }

        #endregion

        #region Inputs

        protected override void HandleShiftInput(IReadOnlyButtonState shiftingState)
        {
            if (shiftingState.WasReleasedThisFrame)
            {
                if (Factory.HasState(PlayerStates.Climbing))
                {
                    if (Ctx.Stamina > 0)
                    {
                        TrySwitchState(PlayerStates.Climbing);
                    }
                }
            }
        }

        protected override void HandleJumpInput(IReadOnlyButtonState jumpingState)
        {
            if (Factory.HasState(PlayerStates.Jumping))
            {
                if (jumpingState.UseBufferedPress())
                {
                    TrySwitchRootState(PlayerStates.Jumping);
                }
            }
        }

        #endregion

        public override void CheckSwitchState()
        {

        }
    }
}