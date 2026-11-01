using Systems.Input;
using UnityEngine;

namespace Entities.Base.States.Base
{
    /// <summary>
    /// A movement sub-state. Only has a movement child slot and routes transitions through its super state.
    /// </summary>
    public abstract class EntityMovementState : EntityBaseState
    {
        protected EntityMovementState(EntityStateMachine machine, EntityStateFactory factory) : base(machine, factory)
        {
            StateType = EntityStateType.Movement;
        }

        #region Recursive Functions

        public override void ExitStates(EntityBaseState nextState)
        {
            if (IsLocked)
            {
                Debug.LogWarning($"<color=red>Attempted to exit state {StateKey} while locked. Ignoring...</color>");
                return;
            }

            _movementSubState?.ExitStates(null);

            _movementSubState = null;

            ExitState(nextState);
        }

        public override void UpdateStates()
        {
            UpdateState();

            EvictIfStale(ref _movementSubState);
            _movementSubState?.UpdateStates();
        }

        public override void FixedUpdateStates()
        {
            FixedUpdateState();

            _movementSubState?.FixedUpdateStates();
        }

        public override void LateUpdateStates()
        {
            LateUpdateState();

            _movementSubState?.LateUpdateStates();
        }

        public override void CheckSwitchStates()
        {
            if (!_isLocked)
                CheckSwitchState();

            if (_movementSubState?.IsLocked != true)
                _movementSubState?.CheckSwitchStates();
        }

        public override void InitializeSubStates()
        {
            InitializeSubState();

            _movementSubState?.InitializeSubStates();
        }

        public override void OnStateEnteredNotification(EntityBaseState stateEntered)
        {
            OnStateEntered(stateEntered);

            _currentSuperState?.OnStateEnteredNotification(stateEntered);

            if (_movementSubState != stateEntered) _movementSubState?.OnStateEnteredNotification(stateEntered);
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

            if (stateInstance?.StateType == EntityStateType.Movement)
            {
                if (desiredState == _movementSubState?.StateKey) return false;
                SwitchMovementSubState(stateInstance);
                return true;
            }

            return false;
        }

        #endregion

        #region InputActions

        public override void HandleInputActions(IInputProvider input)
        {
            HandleInputAction(input);

            _movementSubState?.HandleInputActions(input);
        }

        #endregion
    }
}
