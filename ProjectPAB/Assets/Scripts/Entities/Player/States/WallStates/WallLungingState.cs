using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class WallLungingState : PlayerBaseState
    {
        public WallLungingState(PlayerStateMachine currentContext, PlayerStateFactory charachterStateFactory) : base(currentContext, charachterStateFactory)
        {
            StateKey = PlayerStates.WallLunging;
            StateType = PlayerStateType.Movement;
        }

        private Rigidbody _rb;

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            Ctx.JumpToFallingTime = Ctx.MaxJumpToFallingTime;
            Ctx.WalkJumpToWalledTime = Ctx.MaxWalkJumpToWalledTime;
            Ctx.JumpToWalledTime = Ctx.MaxJumpToWalledTime;

            _rb = Ctx.Rigidbody;

            _rb.linearVelocity = Vector3.zero;
            Vector3 lungeForce = Ctx.JumpDirection * 7f;
            lungeForce += Ctx.WallDetector.WallNormal * -0.5f;

            _rb.AddForce(lungeForce, ForceMode.Impulse);
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
        }

        #region MonoBehaveiours

        public override void UpdateState()
        {
            Ctx.JumpToWalledTime -= Time.deltaTime;
        }

        public override void FixedUpdateState() { }

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