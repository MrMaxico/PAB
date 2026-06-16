using System.Collections.Generic;
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
        public void SetSubState(PlayerBaseState newSubState)
        {
            if (newSubState == null) return;

            PlayerStateType type = newSubState.StateType;

            if (_subStates.TryGetValue(type, out PlayerBaseState oldState))
            {
                oldState.ExitState(newSubState);
            }

            _subStates[type] = newSubState;
            newSubState.SetSuperState(this);
            newSubState.EnterState(oldState);
        }

        protected Dictionary<PlayerStateType, PlayerBaseState> _subStates = new();
        public IReadOnlyDictionary<PlayerStateType, PlayerBaseState> SubStates => _subStates;

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
            foreach (var subState in _subStates.Values)
            {
                if (subState.IsLocked)
                    continue;
                else
                    subState.ExitStates(nextState);
            }

            ExitState(nextState);
        }

        public virtual void UpdateStates()
        {
            UpdateState();

            foreach (var subState in _subStates.Values)
            {
                if (subState != null)
                {
                    if (!Factory.HasState(subState.StateKey))
                    {
                        Debug.LogWarning($"<color=red>State {subState.StateKey} was unregistered while active. Evicting...</color>");
                        subState.ExitState();
                        InitializeSubStates();
                    }
                    else
                    {
                        subState.UpdateStates();
                    }
                }
            }
        }

        public virtual void FixedUpdateStates()
        {
            FixedUpdateState();

            foreach (var subState in _subStates.Values)
            {
                subState.FixedUpdateStates();
            }
        }

        public virtual void LateUpdateStates()
        {
            LateUpdateState();

            foreach (var subState in _subStates.Values)
            {
                subState.LateUpdateStates();
            }
        }

        public virtual void CheckSwitchStates()
        {
            // 1. Create a snapshot to prevent collection modification errors
            var statesToUpdate = new List<PlayerBaseState>(_subStates.Values);

            // 2. Iterate over the snapshot, not the dictionary
            foreach (var subState in statesToUpdate)
            {
                subState?.CheckSwitchStates();
            }

            // 3. Check the current state itself
            CheckSwitchState();
        }

        public virtual void InitializeSubStates()
        {
            InitializeSubState();

            foreach (var subState in _subStates.Values)
            {
                subState?.InitializeSubStates();
            }
        }

        public virtual void OnStateEnteredNotification(PlayerBaseState stateEntered)
        {
            OnStateEntered(stateEntered);

            _currentSuperState?.OnStateEnteredNotification(stateEntered);

            foreach (var subState in _subStates.Values)
            {
                if (subState != stateEntered)
                {
                    subState?.OnStateEnteredNotification(stateEntered);
                }
            }
        }

        protected void OnStateEntered(PlayerBaseState stateEntered) { }

        #endregion

        #region Abstract Functions

        public abstract void UpdateState();
        public abstract void FixedUpdateState();

        public virtual void LateUpdateState() { }

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

            // maybe needs to be persistent if the state is locked, for when base states switch and a child is locked.
            if (stateInstance.IsLocked) return false;

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

            // Root switches route upwards
            if (stateInstance.StateType == PlayerStateType.Root)
            {
                return TrySwitchState(desiredState);
            }

            // Context switches route to the state machine level
            if (stateInstance.StateType == PlayerStateType.Context)
            {
                Ctx.SwitchContextState(desiredState);
                return true;
            }

            // Handle all other runtime sub-states dynamically (Movement, Action, etc.)
            PlayerStateType targetSlot = stateInstance.StateType;

            // Check if this sub-state track is already running the desired state
            if (_subStates.TryGetValue(targetSlot, out PlayerBaseState currentActiveSubState))
            {
                if (desiredState == currentActiveSubState.StateKey) return false;
            }

            // Perform the state swap dynamically
            SetSubState(stateInstance);
            return true;
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

            // Pass the entire dictionary of active sub-states to the new root state
            if (_subStates.Count > 0)
            {
                newState.InheritSubStates(_subStates);
                _subStates.Clear();
            }

            newState.InitializeSubState();
        }

        //protected void SwitchMovementSubState(PlayerBaseState newMovementState)
        //{
        //    _movementSubState?.ExitState(newMovementState);

        //    PlayerBaseState previousState = _movementSubState;

        //    _movementSubState = newMovementState;
        //    newMovementState.SetSuperState(this);

        //    newMovementState.EnterState(previousState);
        //    newMovementState.OnStateEnteredNotification(newMovementState);

        //    newMovementState.InitializeSubStates();
        //}

        //protected void SwitchActionSubState(PlayerBaseState newCombatState)
        //{
        //    _actionSubState?.ExitState(newCombatState);

        //    PlayerBaseState previousState = _actionSubState;

        //    _actionSubState = newCombatState;
        //    newCombatState.SetSuperState(this);

        //    newCombatState.EnterState(previousState);
        //    newCombatState.OnStateEnteredNotification(newCombatState);

        //    newCombatState.InitializeSubStates();
        //}

        public void InheritSubStates(Dictionary<PlayerStateType, PlayerBaseState> previousSubStates)
        {
            foreach (var kvp in previousSubStates)
            {
                if (kvp.Key == PlayerStateType.Root || kvp.Key == PlayerStateType.Context) continue;

                SetSubState(kvp.Value);
            }
        }

        #endregion

        #region InputActions

        protected virtual void HandleInput(IInputProvider inputProvider) { }
        public virtual void HandleInputs(IInputProvider inputProvider)
        {
            HandleInput(inputProvider);

            foreach (var subState in _subStates.Values)
            {
                subState?.HandleInputs(inputProvider);
            }
        }

        #endregion
    }
}