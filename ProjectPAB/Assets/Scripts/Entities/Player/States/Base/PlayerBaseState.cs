using Systems.Input;
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
        public PlayerBaseState MovementSubState
        {
            get => _movementSubState;
            set => _movementSubState = value;
        }

        protected PlayerBaseState _actionSubState;
        public PlayerBaseState ActionSubState
        {
            get => _actionSubState;
            set => _actionSubState = value;
        }

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

        public virtual void UpdateState() { }
        public virtual void FixedUpdateState() { }
        public virtual void LateUpdateState() { }

        public abstract void EnterState(PlayerBaseState previousState = null);
        public abstract void ExitState(PlayerBaseState nextState = null);

        public virtual void InitializeSubState() { }
        public virtual void CheckSwitchState() { }

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
            if (IsLocked)
            {
                Debug.LogWarning($"<color=red>Attempted to switch to root state {newState.StateKey} while in locked state {StateKey}. Ignoring...</color>");
                return;
            }

            newState.InheritLockedSubStatesFrom(this);

            ExitStates(newState);

            PlayerBaseState previousState = _ctx.CurrentState;
            _ctx.CurrentState = newState;

            Ctx.InvokeRootStateTransitioned(newState.StateKey);
            newState.EnterState(previousState);
            newState.OnStateEntered(newState);

            newState.InitializeSubState();
        }

        protected void SwitchMovementSubState(PlayerBaseState newMovementState)
        {
            if (_movementSubState?.IsLocked == true)
            {
                Debug.LogWarning($"<color=red>Attempted to switch sub-state {newMovementState.StateKey} while locked</color>");
                return;
            }

            _movementSubState?.ExitState(newMovementState);

            PlayerBaseState previousState = _movementSubState;

            _movementSubState = newMovementState;
            newMovementState.SetSuperState(this);

            Ctx.InvokeMovementStateTransitioned(newMovementState.StateKey);
            newMovementState.EnterState(previousState);
            newMovementState.OnStateEnteredNotification(newMovementState);

            newMovementState.InitializeSubStates();
        }

        protected void SwitchActionSubState(PlayerBaseState newActionState)
        {
            if (_actionSubState?.IsLocked == true)
            {
                Debug.LogWarning($"<color=red>Attempted to switch sub-state {newActionState.StateKey} while locked</color>");
                return;
            }

            _actionSubState?.ExitState(newActionState);

            PlayerBaseState previousState = _actionSubState;

            _actionSubState = newActionState;
            newActionState.SetSuperState(this);

            Ctx.InvokeActionStateTransitioned(newActionState.StateKey);
            newActionState.EnterState(previousState);
            newActionState.OnStateEnteredNotification(newActionState);

            newActionState.InitializeSubStates();
        }

        public void InheritLockedSubStatesFrom(PlayerBaseState oldState)
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