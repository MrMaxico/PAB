using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class DivingState : MovementBaseState
    {
        public DivingState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Diving;
            StateType = PlayerStateType.Movement;
        }
        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            SetLock(true);
        }
        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
        }

        #region MonoBehaveiours

        public override void UpdateState()
        {

        }

        public override void FixedUpdateState()
        {

        }

        #endregion

        #region Inputs

        #endregion

        #region StateLogic

        public override void CheckSwitchState()
        {

        }

        #endregion
    }
}
