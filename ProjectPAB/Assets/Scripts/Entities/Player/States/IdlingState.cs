using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class IdlingState : PlayerBaseState
    {
        public IdlingState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Idling;
            StateType = PlayerStateType.Movement;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
        }

        #region MonoBehaviours

        public override void UpdateState() { }

        public override void FixedUpdateState()
        {
            if (Ctx.GroundDetector.HasAnyHit())
                HandleDeceleration();
        }

        private void HandleDeceleration()
        {
            Vector3 currentVel = Ctx.Rigidbody.linearVelocity;
            Vector3 targetVel = new Vector3(0, currentVel.y, 0);
            float decelerationSpeed = 15f;
            Ctx.Rigidbody.linearVelocity = Vector3.Lerp(currentVel, targetVel, Time.fixedDeltaTime * decelerationSpeed);
        }

        #endregion

        #region Inputs

        #endregion

        #region State Logic

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Walking))
            {
                if (_currentSuperState.StateKey == PlayerStates.Grounded && Ctx.IsMovementInput)
                {
                    TrySwitchState(PlayerStates.Walking);
                    return;
                }
            }
        }

        #endregion
    }
}
