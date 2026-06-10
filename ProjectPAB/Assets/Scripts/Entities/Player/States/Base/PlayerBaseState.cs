using UnityEngine;

namespace Entities.Player.States.Base
{
    public enum PlayerStateType
    {
        Root,
        Movement,
        Action,
        Context,
    }

    public abstract class PlayerBaseState
    {
        private PlayerStateType _stateType = PlayerStateType.Root;
        public PlayerStateType StateType { get => _stateType; protected set => _stateType = value; }

        private PlayerStates _stateKey;
        public PlayerStates StateKey { get => _stateKey; protected set => _stateKey = value; }

        private PlayerStateMachine _ctx;
        protected PlayerStateMachine Ctx => _ctx;

        private PlayerStateFactory _factory;
        protected PlayerStateFactory Factory => _factory;

        protected PlayerBaseState _currentSuperState;
        public PlayerBaseState CurrentSuperState => _currentSuperState;

        public void SetSuperState(PlayerBaseState superState) => _currentSuperState = superState;

        protected PlayerBaseState _movementSubState;
        public PlayerBaseState MovementSubState => _movementSubState;

        protected PlayerBaseState _actionSubState;
        public PlayerBaseState ActionSubState => _actionSubState;

        protected bool _isLocked = false;
        public bool IsLocked => _isLocked;

        public void SetLock(bool lockState) => _isLocked = lockState;

        public PlayerBaseState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory)
        {
            _ctx = currentContext;
            _factory = stateFactory;
        }

        #region Recursive Functions

        public virtual void ExitStates(PlayerBaseState nextState)
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

            if (_movementSubState != null)
            {
                if (Factory.GetState(_movementSubState.StateKey) == null)
                {
                    Debug.LogWarning($"<color=red>State {_movementSubState.StateKey} was unregistered while active. Evicting...</color>");

                    _movementSubState.ExitState();
                    InitializeSubStates();
                }
                else
                {
                    _movementSubState.UpdateStates();
                }
            }

            if (_actionSubState != null)
            {
                if (Factory.GetState(_actionSubState.StateKey) == null)
                {
                    Debug.LogWarning($"<color=red>State {_actionSubState.StateKey} was unregistered while active. Evicting...</color>");

                    _actionSubState.ExitState();
                    InitializeSubStates();
                }
                else
                {
                    _actionSubState.UpdateStates();
                }
            }
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

        public virtual void OnStateEnteredNotification(PlayerBaseState stateEntered)
        {
            OnStateEntered(stateEntered);

            _currentSuperState?.OnStateEnteredNotification(stateEntered);

            if (_movementSubState != stateEntered) _movementSubState?.OnStateEnteredNotification(stateEntered);
            if (_actionSubState != stateEntered) _actionSubState?.OnStateEnteredNotification(stateEntered);
        }

        protected void OnStateEntered(PlayerBaseState stateEntered) { }

        #endregion

        #region Abstract Functions

        public abstract void UpdateState();
        public abstract void FixedUpdateState();
        public abstract void LateUpdateState();

        public virtual void InitializeSubState() { }

        public abstract void EnterState(PlayerBaseState previousState = null);
        public abstract void ExitState(PlayerBaseState nextState = null);

        public abstract void CheckSwitchState();

        #endregion

        #region State Switching

        public virtual bool TrySwitchState(PlayerStates desiredState)
        {
            if (desiredState == StateKey) return false;

            if (_currentSuperState != null)
            {
                return _currentSuperState.TrySwitchState(desiredState);
            }

            PlayerBaseState stateInstance = Factory.GetState(desiredState);
            if (stateInstance == null) return false;

            if (stateInstance.StateType == PlayerStateType.Root)
            {
                SwitchRootState(stateInstance);
                return true;
            }

            // Context switches go through the state machine
            if (stateInstance.StateType == PlayerStateType.Context)
            {
                Ctx.SwitchContextState(desiredState);
                return true;
            }

            return TrySwitchSubState(desiredState);
        }

        public virtual bool TrySwitchSubState(PlayerStates desiredState)
        {
            PlayerBaseState stateInstance = Factory.GetState(desiredState);
            if (stateInstance == null) return false;

            if (stateInstance.StateType == PlayerStateType.Root)
            {
                return TrySwitchState(desiredState);
            }

            // Context switches go through the state machine
            if (stateInstance.StateType == PlayerStateType.Context)
            {
                Ctx.SwitchContextState(desiredState);
                return true;
            }

            switch (stateInstance.StateType)
            {
                case PlayerStateType.Movement:
                    if (desiredState == _movementSubState?.StateKey) return false;
                    SwitchMovementSubState(stateInstance);
                    return true;
                case PlayerStateType.Action:
                    if (desiredState == _actionSubState?.StateKey) return false;
                    SwitchActionSubState(stateInstance);
                    return true;
            }
            return false;
        }

        protected bool TrySwitchRootState(PlayerStates desiredState)
        {
            PlayerBaseState root = this;

            while (root.CurrentSuperState != null)
            {
                root = root.CurrentSuperState;
            }

            return root.TrySwitchState(desiredState);
        }

        private void SwitchRootState(PlayerBaseState newState)
        {
            ExitStates(newState);

            PlayerBaseState previousState = _ctx.CurrentState;
            _ctx.CurrentState = newState;

            newState.EnterState(previousState);
            newState.OnStateEntered(newState);

            newState.InitializeSubState();
        }

        protected void SwitchMovementSubState(PlayerBaseState newMovementState)
        {
            _movementSubState?.ExitState(newMovementState);

            PlayerBaseState previousState = _movementSubState;

            _movementSubState = newMovementState;
            newMovementState.SetSuperState(this);

            newMovementState.EnterState(previousState);
            newMovementState.OnStateEnteredNotification(newMovementState);

            newMovementState.InitializeSubStates();
        }

        protected void SwitchActionSubState(PlayerBaseState newCombatState)
        {
            _actionSubState?.ExitState(newCombatState);

            PlayerBaseState previousState = _actionSubState;

            _actionSubState = newCombatState;
            newCombatState.SetSuperState(this);

            newCombatState.EnterState(previousState);
            newCombatState.OnStateEnteredNotification(newCombatState);

            newCombatState.InitializeSubStates();
        }

        #endregion

        #region InputActions

        protected virtual void HandleJumpInput(bool isJumping) { }
        public virtual void HandleJumpInputs(bool isJumping)
        {
            HandleJumpInput(isJumping);

            _movementSubState?.HandleJumpInputs(isJumping);
            _actionSubState?.HandleJumpInputs(isJumping);
        }

        protected virtual void HandleMoveInput(Vector2 movement) { }
        public virtual void HandleMoveInputs(Vector2 movement)
        {
            HandleMoveInput(movement);

            _movementSubState?.HandleMoveInputs(movement);
            _actionSubState?.HandleMoveInputs(movement);
        }

        protected virtual void HandleRunInput(bool isRunning) { }
        public virtual void HandleRunInputs(bool isRunning)
        {
            HandleRunInput(isRunning);

            _movementSubState?.HandleRunInputs(isRunning);
            _actionSubState?.HandleRunInputs(isRunning);
        }

        protected virtual void HandleShiftInput(bool isShifting) { }
        public virtual void HandleShiftInputs(bool isShifting)
        {
            HandleShiftInput(isShifting);

            _movementSubState?.HandleShiftInputs(isShifting);
            _actionSubState?.HandleShiftInputs(isShifting);
        }

        protected virtual void HandleShootInput(bool isShooting) { }
        public virtual void HandleShootInputs(bool isShooting)
        {
            HandleShootInput(isShooting);

            _movementSubState?.HandleShootInputs(isShooting);
            _actionSubState?.HandleShootInputs(isShooting);
        }

        #endregion
    }
}