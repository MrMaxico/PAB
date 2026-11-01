using Systems.Input;

namespace Entities.Base.States.Base
{
    /// <summary>
    /// A context state (e.g. camera modes). Lives on the state machine next to the root state,
    /// persists across root transitions, and can nest its own context children.
    /// </summary>
    public abstract class EntityContextState : EntityBaseState
    {
        protected EntityBaseState _contextSubState;
        public EntityBaseState ContextSubState => _contextSubState;

        protected EntityContextState(EntityStateMachine machine, EntityStateFactory factory) : base(machine, factory)
        {
            StateType = EntityStateType.Context;
        }

        #region Recursive Functions

        public override void ExitStates(EntityBaseState nextState)
        {
            _contextSubState?.ExitStates(null);
            _contextSubState = null;

            ExitState(nextState);
        }

        public override void UpdateStates()
        {
            UpdateState();

            EvictIfStale(ref _contextSubState);
            _contextSubState?.UpdateStates();
        }

        public override void FixedUpdateStates()
        {
            FixedUpdateState();

            _contextSubState?.FixedUpdateStates();
        }

        public override void LateUpdateStates()
        {
            LateUpdateState();

            _contextSubState?.LateUpdateStates();
        }

        public override void CheckSwitchStates()
        {
            if (!_isLocked)
                CheckSwitchState();

            if (_contextSubState?.IsLocked != true)
                _contextSubState?.CheckSwitchStates();
        }

        public override void InitializeSubStates()
        {
            InitializeSubState();

            _contextSubState?.InitializeSubStates();
        }

        public override void OnStateEnteredNotification(EntityBaseState stateEntered)
        {
            OnStateEntered(stateEntered);

            if (_contextSubState != stateEntered) _contextSubState?.OnStateEnteredNotification(stateEntered);
        }

        #endregion

        #region State Switching

        public override bool TrySwitchState(StateKey desiredState)
        {
            EntityBaseState stateInstance = Factory.GetState(desiredState);
            if (stateInstance == null) return false;

            // Root transitions go through the root state
            if (stateInstance.StateType == EntityStateType.Root)
            {
                return TrySwitchRootState(desiredState);
            }

            // Top-level context swap goes through the state machine
            if (stateInstance.StateType == EntityStateType.Context)
            {
                Machine.SwitchContextState(desiredState);
                return true;
            }

            return false;
        }

        public override bool TrySwitchSubState(StateKey desiredState)
        {
            EntityBaseState stateInstance = Factory.GetState(desiredState);
            if (stateInstance == null) return false;

            if (stateInstance.StateType == EntityStateType.Root)
            {
                return TrySwitchRootState(desiredState);
            }

            // Nested context children
            if (stateInstance.StateType == EntityStateType.Context)
            {
                if (desiredState == _contextSubState?.StateKey) return false;
                SwitchContextSubState(stateInstance);
                return true;
            }

            return false;
        }

        protected void SwitchContextSubState(EntityBaseState newContextState)
        {
            _contextSubState?.ExitState(newContextState);
            EntityBaseState previousState = _contextSubState;
            _contextSubState = newContextState;
            newContextState.SetSuperState(this);

            newContextState.EnterState(previousState);
            newContextState.OnStateEnteredNotification(newContextState);

            newContextState.InitializeSubStates();
        }

        #endregion

        #region InputActions

        public override void HandleInputActions(IInputProvider input)
        {
            HandleInputAction(input);

            _contextSubState?.HandleInputActions(input);
        }

        #endregion
    }
}
