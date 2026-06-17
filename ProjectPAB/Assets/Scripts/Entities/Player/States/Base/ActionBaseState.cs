using Systems.Input;
using UnityEngine;

namespace Entities.Player.States.Base
{
    public abstract class ActionBaseState : PlayerBaseState
    {
        public ActionBaseState(PlayerStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory)
        {
            StateType = PlayerStateType.Action;
        }

        #region Recursive Functions

        public override void ExitStates(PlayerBaseState nextState)
        {
            _actionSubState?.ExitStates(null);

            _actionSubState = null;

            ExitState(nextState);
        }

        public override void UpdateStates()
        {
            UpdateState();

            if (_actionSubState != null)
            {
                if (Factory.GetState(_actionSubState.StateKey) == null)
                {
                    Debug.LogWarning($"<color=red>State {_actionSubState.StateKey} was unregistered while active. Evicting...</color>");

                    _actionSubState.ExitStates(null);
                    InitializeSubStates();
                }
                else
                {
                    _actionSubState?.UpdateStates();
                }
            }
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

        public override void OnStateEnteredNotification(PlayerBaseState stateEntered)
        {
            OnStateEntered(stateEntered);

            if (_actionSubState != stateEntered) _actionSubState?.OnStateEnteredNotification(stateEntered);
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

            if (stateInstance?.StateType == PlayerStateType.Action)
            {
                if (desiredState == _actionSubState?.StateKey) return false;
                SwitchActionSubState(stateInstance);
                return true;
            }

            return false;
        }

        #endregion

        #region InputActions

        public override void HandleJumpInputs(IReadOnlyButtonState jumpingState)
        {
            HandleJumpInput(jumpingState);

            _actionSubState?.HandleJumpInputs(jumpingState);
        }

        public override void HandleMoveInputs(IReadOnlyMovementInputState movementState)
        {
            HandleMoveInput(movementState);

            _actionSubState?.HandleMoveInputs(movementState);
        }

        public override void HandleRunInputs(IReadOnlyButtonState runningState)
        {
            HandleRunInput(runningState);

            _actionSubState?.HandleRunInputs(runningState);
        }

        public override void HandleShiftInputs(IReadOnlyButtonState shiftingState)
        {
            HandleShiftInput(shiftingState);

            _actionSubState?.HandleShiftInputs(shiftingState);
        }

        public override void HandleShootInputs(IReadOnlyButtonState shootingState)
        {
            HandleShootInput(shootingState);

            _actionSubState?.HandleShootInputs(shootingState);
        }

        public override void HandleSlideInputs(IReadOnlyButtonState slidingState)
        {
            HandleSlideInputs(slidingState);

            _actionSubState?.HandleSlideInputs(slidingState);
        }

        #endregion
    }
}