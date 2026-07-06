using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class WaterborneState : PlayerBaseState
    {
        public WaterborneState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Waterborne;
        }

        public override void EnterState(PlayerBaseState previousState = null)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.Rigidbody.useGravity = false;
            Ctx.Rigidbody.linearVelocity = Vector3.zero;
        }

        public override void ExitState(PlayerBaseState nextState = null)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif
        }
    }
}
