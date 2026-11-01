using Systems.Input;

namespace Entities.Base.States.Base
{
    /// <summary>
    /// An action sub-state (attacks, abilities). Only has an action child slot
    /// and routes transitions through its super state.
    /// </summary>
    public abstract class EntityActionState : EntityBaseState
    {
        protected EntityActionState(EntityStateMachine machine, EntityStateFactory factory) : base(machine, factory)
        {
            StateType = EntityStateType.Action;
        }

        #region Recursive Functions

        public override void ExitStates(EntityBaseState nextState)
        {
            _actionSubState?.ExitStates(null);

            _actionSubState = null;

            ExitState(nextState);
        }

        public override void UpdateStates()
        {
            UpdateState();

            EvictIfStale(ref _actionSubState);
            _actionSubState?.UpdateStates();
        }

        public override void FixedUpdateStates()
        {
            FixedUpdateState();

            _actionSubState?.FixedUpdateStates();
        }

        public override void LateUpdateStates()
        {
            LateUpdateState();

            _actionSubState?.LateUpdateStates();
        }

        public override void CheckSwitchStates()
        {
            if (!_isLocked)
                CheckSwitchState();

            if (_actionSubState?.IsLocked != true)
                _actionSubState?.CheckSwitchStates();
        }

        public override void InitializeSubStates()
        {
            InitializeSubState();

            _actionSubState?.InitializeSubStates();
        }

        public override void OnStateEnteredNotification(EntityBaseState stateEntered)
        {
            OnStateEntered(stateEntered);

            if (_actionSubState != stateEntered) _actionSubState?.OnStateEnteredNotification(stateEntered);
        }

        #endregion

        #region State Switching

        public override bool TrySwitchState(StateKey desiredState)
        {
            return _currentSuperState != null && _currentSuperState.TrySwitchSubState(desiredState);
        }

        public override bool TrySwitchSubState(StateKey desiredState)
        {
            EntityBaseState stateInstance = Factory.GetState(desiredState);

            if (stateInstance?.StateType == EntityStateType.Root) return TrySwitchRootState(desiredState);

            if (stateInstance?.StateType == EntityStateType.Action)
            {
                if (desiredState == _actionSubState?.StateKey) return false;
                SwitchActionSubState(stateInstance);
                return true;
            }

            return false;
        }

        #endregion

        #region InputActions

        public override void HandleInputActions(IInputProvider input)
        {
            HandleInputAction(input);

            _actionSubState?.HandleInputActions(input);
        }

        #endregion
    }
}
