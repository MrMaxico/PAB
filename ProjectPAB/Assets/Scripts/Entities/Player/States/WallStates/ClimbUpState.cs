using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class ClimbUpState : MovementBaseState
    {
        public ClimbUpState(PlayerStateMachine currentContext, PlayerStateFactory charachterStateFactory) : base(currentContext, charachterStateFactory)
        {
            StateKey = PlayerStates.ClimbUp;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState.StateKey}");
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState.StateKey}");

            Ctx.Rigidbody.isKinematic = false;
        }

        #region MonoBehaviours

        public override void UpdateState()
        {
            Ctx.transform.position = Ctx.WallDetector.WallHit.point + Vector3.up * 1.5f;
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
            if (Factory.HasState(PlayerStates.Grounded))
            {
                if (Ctx.GroundDetector.HasAnyHit())
                {
                    TrySwitchRootState(PlayerStates.Grounded);
                }
            }
        }
    }
}