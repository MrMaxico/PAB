using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class IdlingState : MovementBaseState
    {
        private Vector2 _moveInput;

        public IdlingState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Idling;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif
        }

        public override void ExitState(PlayerBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif
        }

        #region MonoBehaviours

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

        protected override void HandleInputAction(IInputProvider input)
        {
            _moveInput = input.MovementState.RawInputValue;
        }

        #endregion

        #region State Logic

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Walking))
            {
                if (Ctx.IsMovementInput && Ctx.GroundDetector.HasAnyHit())
                {
                    if (TrySwitchState(PlayerStates.Walking))
                        return;
                }
            }
        }

        #endregion
    }
}
