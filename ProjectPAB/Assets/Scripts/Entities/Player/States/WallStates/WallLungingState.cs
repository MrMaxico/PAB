using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class WallLungingState : MovementBaseState
    {
        public WallLungingState(PlayerStateMachine currentContext, PlayerStateFactory charachterStateFactory) : base(currentContext, charachterStateFactory)
        {
            StateKey = PlayerStates.WallLunging;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.JumpToFallingTime = Ctx.MaxJumpToFallingTime;
            Ctx.WalkJumpToWalledTime = Ctx.MaxWalkJumpToWalledTime;
            Ctx.JumpToWalledTime = Ctx.MaxJumpToWalledTime;

            Ctx.Rigidbody.linearVelocity = Vector3.zero;

            Vector3 lungeForce = Ctx.JumpDirection * 7f;
            lungeForce += Ctx.WallDetector.WallNormal * -0.5f;

            Vector3 vel = Ctx.Rigidbody.linearVelocity;
            vel.y = 0;
            Ctx.Rigidbody.linearVelocity = vel;

            Ctx.Rigidbody.AddForce(lungeForce, ForceMode.Impulse);
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
            Ctx.JumpToWalledTime -= Time.deltaTime;
        }

        public override void FixedUpdateState()
        {

        }

        public override void LateUpdateState()
        {

        }

        #endregion

        #region Inputs

        #endregion

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Climbing))
            {
                if (Ctx.JumpToWalledTime <= 0)
                {
                    TrySwitchState(PlayerStates.Climbing);
                }
            }
        }
    }
}