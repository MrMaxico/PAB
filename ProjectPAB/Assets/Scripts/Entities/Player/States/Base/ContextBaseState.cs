namespace Entities.Player.States.Base
{
    public abstract class ContextBaseState : PlayerBaseState
    {
        public ContextBaseState(PlayerStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory)
        {
            StateType = PlayerStateType.Context;
        }

        // We ONLY override this to prevent nested context states from being kicked up to the StateMachine level.
        public override bool TrySwitchSubState(PlayerStates desiredState)
        {
            PlayerBaseState stateInstance = Factory.GetState(desiredState);
            if (stateInstance == null) return false;

            if (stateInstance.StateType == PlayerStateType.Root)
            {
                return TrySwitchRootState(desiredState);
            }

            // Intercept Context type and nest it in our own dictionary
            if (stateInstance.StateType == PlayerStateType.Context)
            {
                // Check if we are already in this state
                if (_subStates.TryGetValue(PlayerStateType.Context, out PlayerBaseState currentSub) && currentSub.StateKey == desiredState)
                    return false;

                SetSubState(stateInstance);
                return true;
            }

            // For Movement or Action sub-states, let the base class handle it
            return base.TrySwitchSubState(desiredState);
        }
    }
}