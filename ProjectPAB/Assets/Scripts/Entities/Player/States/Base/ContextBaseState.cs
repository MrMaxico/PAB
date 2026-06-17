using Systems.Input;
using UnityEngine;

namespace Entities.Player.States.Base
{
    public abstract class ContextBaseState : PlayerBaseState
    {
        protected PlayerBaseState _contextSubState;
        public PlayerBaseState ContextSubState => _contextSubState;

        public ContextBaseState(PlayerStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory)
        {
            StateType = PlayerStateType.Context;
        }

        #region Recursive Functions

        public override void ExitStates(PlayerBaseState nextState)
        {
            _contextSubState?.ExitStates(null);
            _contextSubState = null;

            ExitState(nextState);
        }

        public override void UpdateStates()
        {
            UpdateState();

            if (_contextSubState != null)
            {
                if (Factory.GetState(_contextSubState.StateKey) == null)
                {
                    Debug.LogWarning($"<color=red>State {_contextSubState.StateKey} was unregistered while active. Evicting...</color>");

                    _contextSubState.ExitStates(null);
                    InitializeSubStates();
                }
                else
                {
                    _contextSubState.UpdateStates();
                }
            }
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

        public override void OnStateEnteredNotification(PlayerBaseState stateEntered)
        {
            OnStateEntered(stateEntered);

            if (_contextSubState != stateEntered) _contextSubState?.OnStateEnteredNotification(stateEntered);
        }

        #endregion

        #region State Switching

        public override bool TrySwitchState(PlayerStates desiredState)
        {
            PlayerBaseState stateInstance = Factory.GetState(desiredState);
            if (stateInstance == null) return false;

            // Root transitions go through the state machine
            if (stateInstance.StateType == PlayerStateType.Root)
            {
                return TrySwitchRootState(desiredState);
            }

            // Top-level context swap goes through the state machine
            if (stateInstance.StateType == PlayerStateType.Context)
            {
                Ctx.SwitchContextState(desiredState);
                return true;
            }

            return false;
        }

        public override bool TrySwitchSubState(PlayerStates desiredState)
        {
            PlayerBaseState stateInstance = Factory.GetState(desiredState);
            if (stateInstance == null) return false;

            if (stateInstance.StateType == PlayerStateType.Root)
            {
                return TrySwitchRootState(desiredState);
            }

            // Nested context children
            if (stateInstance.StateType == PlayerStateType.Context)
            {
                if (desiredState == _contextSubState?.StateKey) return false;
                SwitchContextSubState(stateInstance);
                return true;
            }

            return false;
        }

        protected void SwitchContextSubState(PlayerBaseState newContextState)
        {
            _contextSubState?.ExitState(newContextState);
            PlayerBaseState previousState = _contextSubState;
            _contextSubState = newContextState;
            newContextState.SetSuperState(this);

            newContextState.EnterState(previousState);
            newContextState.OnStateEnteredNotification(newContextState);

            newContextState.InitializeSubStates();
        }

        #endregion

        #region InputActions

        public override void HandleJumpInputs(IReadOnlyButtonState jumpingState)
        {
            HandleJumpInput(jumpingState);

            _contextSubState?.HandleJumpInputs(jumpingState);
        }

        public override void HandleMoveInputs(IReadOnlyMovementInputState movementState)
        {
            HandleMoveInput(movementState);

            _contextSubState?.HandleMoveInputs(movementState);
        }

        public override void HandleRunInputs(IReadOnlyButtonState runningState)
        {
            HandleRunInput(runningState);

            _contextSubState?.HandleRunInputs(runningState);
        }

        public override void HandleShiftInputs(IReadOnlyButtonState shiftingState)
        {
            HandleShiftInput(shiftingState);

            _contextSubState?.HandleShiftInputs(shiftingState);
        }

        public override void HandleShootInputs(IReadOnlyButtonState shootingState)
        {
            HandleShootInput(shootingState);

            _contextSubState?.HandleShootInputs(shootingState);
        }

        public override void HandleSlideInputs(IReadOnlyButtonState slidingState)
        {
            HandleSlideInput(slidingState);

            _contextSubState?.HandleSlideInputs(slidingState);
        }

        #endregion
    }
}
