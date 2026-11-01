using Entities.Base.States.Base;
using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class ClimbUpState : PlayerMovementState
    {
        public ClimbUpState(PlayerStateMachine currentContext, PlayerStateFactory charachterStateFactory) : base(currentContext, charachterStateFactory)
        {
            StateKey = PlayerStates.ClimbUp;
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

            Ctx.Rigidbody.isKinematic = false;
        }

        #region MonoBehaviours

        public override void UpdateState()
        {
            Ctx.Rigidbody.position = Ctx.Walls.Hit.point + Vector3.up * 1.5f;
        }

        #endregion

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Grounded))
            {
                if (Ctx.Ground.IsHit)
                {
                    TrySwitchRootState(PlayerStates.Grounded);
                }
            }
        }
    }
}