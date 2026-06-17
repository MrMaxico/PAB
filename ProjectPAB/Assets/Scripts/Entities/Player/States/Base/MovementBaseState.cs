using Systems.Input;
using UnityEngine;

namespace Entities.Player.States.Base
{
    public abstract class MovementBaseState : PlayerBaseState
    {
        public MovementBaseState(PlayerStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory)
        {
            StateType = PlayerStateType.Movement;
        }

        #region Recursive Functions

        public override void ExitStates(PlayerBaseState nextState)
        {
            _movementSubState?.ExitStates(null);

            _movementSubState = null;

            ExitState(nextState);
        }

        public override void UpdateStates()
        {
            UpdateState();

            if (_movementSubState != null)
            {
                if (Factory.GetState(_movementSubState.StateKey) == null)
                {
                    Debug.LogWarning($"<color=red>State {_movementSubState.StateKey} was unregistered while active. Evicting...</color>");

                    _movementSubState.ExitStates(null);
                    InitializeSubStates();
                }
                else
                {
                    _movementSubState?.UpdateStates();
                }
            }
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

        public override void OnStateEnteredNotification(PlayerBaseState stateEntered)
        {
            OnStateEntered(stateEntered);

            if (_movementSubState != stateEntered) _movementSubState?.OnStateEnteredNotification(stateEntered);
        }

        #endregion

        #region State Switching

        public override bool TrySwitchState(PlayerStates desiredState)
        {
            return _currentSuperState != null && _currentSuperState.TrySwitchSubState(desiredState);
        }

        public override bool TrySwitchSubState(PlayerStates desiredState)
        {
            PlayerBaseState stateInstance = Factory.GetState(desiredState);

            if (stateInstance?.StateType == PlayerStateType.Root) return TrySwitchRootState(desiredState);

            if (stateInstance?.StateType == PlayerStateType.Movement)
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