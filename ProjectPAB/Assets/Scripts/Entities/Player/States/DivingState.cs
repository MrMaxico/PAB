using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class DivingState : MovementBaseState
    {
        private float _diveDuration;
        private float _maxDiveDuration = 1.5f;

        public DivingState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Diving;
            StateType = PlayerStateType.Movement;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.LogWarning($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            _diveDuration = _maxDiveDuration;

            SetLock(true);
        }
        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.LogError($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
        }

        #region MonoBehaveiours

        public override void FixedUpdateState()
        {
            if (_diveDuration > 0)
            {
                _diveDuration -= Time.fixedDeltaTime;
            }
            else
            {
                _diveDuration = 0;
                SetLock(false);
            }
        }

        #endregion

        #region Inputs

        #endregion

        #region StateLogic

        #endregion
    }
}
