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

        public override void HandleJumpInputs(bool isJumping)
        {
            HandleJumpInput(isJumping);

            _actionSubState?.HandleJumpInputs(isJumping);
        }

        public override void HandleMoveInputs(Vector2 movement)
        {
            HandleMoveInput(movement);

            _actionSubState?.HandleMoveInputs(movement);
        }

        public override void HandleRunInputs(bool isRunning)
        {
            HandleRunInput(isRunning);

            _actionSubState?.HandleRunInputs(isRunning);
        }

        public override void HandleShiftInputs(bool isShifting)
        {
            HandleShiftInput(isShifting);

            _actionSubState?.HandleShiftInputs(isShifting);
        }

        public override void HandleShootInputs(bool isShooting)
        {
            HandleShootInput(isShooting);

            _actionSubState?.HandleShootInputs(isShooting);
        }


        #endregion
    }
}