using Systems.Input;
using UnityEngine;

namespace Entities.Base.States.Base
{
    public enum EntityStateType
    {
        Root,
        Movement,
        Action,
        Context,
    }

    public abstract class EntityBaseState
    {
        public EntityStateType StateType { get; protected set; } = EntityStateType.Root;
        public StateKey StateKey { get; protected set; }

        protected EntityStateMachine Machine { get; }
        protected EntityStateFactory Factory { get; }

        protected EntityBaseState _currentSuperState;
        public EntityBaseState CurrentSuperState => _currentSuperState;
        public void SetSuperState(EntityBaseState superState) => _currentSuperState = superState;

        protected EntityBaseState _movementSubState;
        public EntityBaseState MovementSubState
        {
            get => _movementSubState;
            set => _movementSubState = value;
        }

        protected EntityBaseState _actionSubState;
        public EntityBaseState ActionSubState
        {
            get => _actionSubState;
            set => _actionSubState = value;
        }

        protected bool _isLocked = false;
        public bool IsLocked => _isLocked;
        public void SetLock(bool lockState) => _isLocked = lockState;

        protected EntityBaseState(EntityStateMachine machine, EntityStateFactory factory)
        {
            Machine = machine;
            Factory = factory;
        }

        #region Recursive Functions

        public virtual void ExitStates(EntityBaseState nextState)
        {
            _movementSubState?.ExitStates(null);
            _actionSubState?.ExitStates(null);

            _movementSubState = null;
            _actionSubState = null;

            ExitState(nextState);
        }

        public virtual void UpdateStates()
        {
            UpdateState();

            EvictIfStale(ref _movementSubState);
            _movementSubState?.UpdateStates();

            EvictIfStale(ref _actionSubState);
            _actionSubState?.UpdateStates();
        }

        protected void EvictIfStale(ref EntityBaseState subState)
        {
            if (subState == null) return;
            if (Factory.GetState(subState.StateKey) == subState) return;

            Debug.LogWarning($"<color=red>State {subState.StateKey} was removed or replaced while active. Evicting...</color>");

            EntityBaseState evicted = subState;
            subState = null;          // clear first so re-initialization doesn't exit it a second time
            evicted.ExitStates(null); // exits the evicted state's own children too

            InitializeSubState();
        }

        public virtual void FixedUpdateStates()
        {
            FixedUpdateState();

            _movementSubState?.FixedUpdateStates();
            _actionSubState?.FixedUpdateStates();
        }

        public virtual void LateUpdateStates()
        {
            LateUpdateState();

            _movementSubState?.LateUpdateStates();
            _actionSubState?.LateUpdateStates();
        }

        public virtual void CheckSwitchStates()
        {
            if (!_isLocked)
                CheckSwitchState();

            if (_movementSubState?.IsLocked != true)
                _movementSubState?.CheckSwitchStates();

            if (_actionSubState?.IsLocked != true)
                _actionSubState?.CheckSwitchStates();
        }

        public virtual void InitializeSubStates()
        {
            InitializeSubState();

            _movementSubState?.InitializeSubStates();
            _actionSubState?.InitializeSubStates();
        }

        public virtual void OnStateEnteredNotification(EntityBaseState stateEntered)
        {
            OnStateEntered(stateEntered);

            _currentSuperState?.OnStateEnteredNotification(stateEntered);

            if (_movementSubState != stateEntered) _movementSubState?.OnStateEnteredNotification(stateEntered);
            if (_actionSubState != stateEntered) _actionSubState?.OnStateEnteredNotification(stateEntered);
        }

        protected virtual void OnStateEntered(EntityBaseState stateEntered) { }

        #endregion

        #region Abstract Functions

        public virtual void UpdateState() { }
        public virtual void FixedUpdateState() { }
        public virtual void LateUpdateState() { }

        public abstract void EnterState(EntityBaseState previousState = null);
        public abstract void ExitState(EntityBaseState nextState = null);

        public virtual void InitializeSubState() { }
        public virtual void CheckSwitchState() { }

        #endregion

        #region State Switching

        public virtual bool TrySwitchState(StateKey desiredState)
        {
            if (desiredState == StateKey) return false;

            if (_currentSuperState != null)
            {
                return _currentSuperState.TrySwitchState(desiredState);
            }

            EntityBaseState stateInstance = Factory.GetState(desiredState);
            if (stateInstance == null) return false;

            if (stateInstance.StateType == EntityStateType.Root)
            {
                SwitchRootState(stateInstance);
                return true;
            }

            if (stateInstance.StateType == EntityStateType.Context)
            {
                Machine.SwitchContextState(desiredState);
                return true;
            }

            return TrySwitchSubState(desiredState);
        }

        public virtual bool TrySwitchSubState(StateKey desiredState)
        {
            EntityBaseState stateInstance = Factory.GetState(desiredState);
            if (stateInstance == null) return false;

            switch (stateInstance.StateType)
            {
                case EntityStateType.Root:
                    return TrySwitchState(desiredState);

                case EntityStateType.Context:
                    Machine.SwitchContextState(desiredState);
                    return true;

                case EntityStateType.Movement:
                    if (desiredState == _movementSubState?.StateKey) return false;
                    SwitchMovementSubState(stateInstance);
                    return true;

                case EntityStateType.Action:
                    if (desiredState == _actionSubState?.StateKey) return false;
                    SwitchActionSubState(stateInstance);
                    return true;
            }
            return false;
        }

        protected bool TrySwitchRootState(StateKey desiredState)
        {
            EntityBaseState root = this;

            while (root.CurrentSuperState != null)
            {
                root = root.CurrentSuperState;
            }

            return root.TrySwitchState(desiredState);
        }

        private void SwitchRootState(EntityBaseState newState)
        {
            if (IsLocked)
            {
                Debug.LogWarning($"<color=red>Attempted to switch to root state {newState.StateKey} while in locked state {StateKey}. Ignoring...</color>");
                return;
            }

            newState.InheritLockedSubStatesFrom(this);

            ExitStates(newState);

            EntityBaseState previousState = Machine.CurrentState;
            Machine.CurrentState = newState;

            Machine.InvokeRootStateTransitioned(newState.StateKey);
            newState.EnterState(previousState);
            newState.OnStateEntered(newState);

            newState.InitializeSubState();
        }

        protected void SwitchMovementSubState(EntityBaseState newMovementState)
        {
            if (_movementSubState?.IsLocked == true)
            {
                Debug.LogWarning($"<color=red>Attempted to switch sub-state {newMovementState.StateKey} while locked</color>");
                return;
            }

            _movementSubState?.ExitState(newMovementState);

            EntityBaseState previousState = _movementSubState;

            _movementSubState = newMovementState;
            newMovementState.SetSuperState(this);

            Machine.InvokeMovementStateTransitioned(newMovementState.StateKey);
            newMovementState.EnterState(previousState);
            newMovementState.OnStateEnteredNotification(newMovementState);

            newMovementState.InitializeSubStates();
        }

        protected void SwitchActionSubState(EntityBaseState newActionState)
        {
            if (_actionSubState?.IsLocked == true)
            {
                Debug.LogWarning($"<color=red>Attempted to switch sub-state {newActionState.StateKey} while locked</color>");
                return;
            }

            _actionSubState?.ExitState(newActionState);

            EntityBaseState previousState = _actionSubState;

            _actionSubState = newActionState;
            newActionState.SetSuperState(this);

            Machine.InvokeActionStateTransitioned(newActionState.StateKey);
            newActionState.EnterState(previousState);
            newActionState.OnStateEnteredNotification(newActionState);

            newActionState.InitializeSubStates();
        }

        public void InheritLockedSubStatesFrom(EntityBaseState oldState)
        {
            if (oldState.MovementSubState?.IsLocked == true)
            {
                _movementSubState = oldState.MovementSubState;
                _movementSubState.SetSuperState(this);

                oldState._movementSubState = null;
            }

            if (oldState.ActionSubState?.IsLocked == true)
            {
                _actionSubState = oldState.ActionSubState;
                _actionSubState.SetSuperState(this);

                oldState._actionSubState = null;
            }
        }

        #endregion

        #region InputActions

        protected virtual void HandleInputAction(IInputProvider input) { }

        public virtual void HandleInputActions(IInputProvider input)
        {
            HandleInputAction(input);

            _movementSubState?.HandleInputActions(input);
            _actionSubState?.HandleInputActions(input);
        }

        #endregion
    }
}
