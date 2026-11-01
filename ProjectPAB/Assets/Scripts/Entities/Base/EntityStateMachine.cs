using Entities.Base.States;
using Entities.Base.States.Base;
using System;
using Systems.Input;
using UnityEngine;

namespace Entities.Base
{
    /// <summary>
    /// Owns the active root and context states of an entity and drives them.
    /// Subclasses create their factory, pick a start state, and call the
    /// <c>*ActiveStates</c> helpers from their Unity callbacks.
    /// </summary>
    public abstract class EntityStateMachine : MonoBehaviour
    {
        public EntityStateFactory Factory { get; protected set; }

        public EntityBaseState CurrentState { get; set; }

        public EntityBaseState CurrentContextState { get; private set; }

        #region StateTransitionEvents

        public event Action<StateKey> OnRootStateTransitioned;
        public event Action<StateKey> OnMovementStateTransitioned;
        public event Action<StateKey> OnActionStateTransitioned;
        public event Action<StateKey> OnContextStateTransitioned;

        public void InvokeRootStateTransitioned(StateKey state) => OnRootStateTransitioned?.Invoke(state);
        public void InvokeMovementStateTransitioned(StateKey state) => OnMovementStateTransitioned?.Invoke(state);
        public void InvokeActionStateTransitioned(StateKey state) => OnActionStateTransitioned?.Invoke(state);
        public void InvokeContextStateTransitioned(StateKey state) => OnContextStateTransitioned?.Invoke(state);

        #endregion

        #region Tick Helpers

        protected void HandleActiveStateInputs(IInputProvider input)
        {
            CurrentState?.HandleInputActions(input);
            CurrentContextState?.HandleInputActions(input);
        }

        protected void UpdateActiveStates()
        {
            CurrentState?.UpdateStates();
            CurrentContextState?.UpdateStates();
        }

        protected void FixedUpdateActiveStates()
        {
            CurrentState?.FixedUpdateStates();
            CurrentContextState?.FixedUpdateStates();
        }

        protected void CheckActiveStateSwitches()
        {
            CurrentState?.CheckSwitchStates();
            CurrentContextState?.CheckSwitchStates();
        }

        protected void LateUpdateActiveStates()
        {
            CurrentState?.LateUpdateStates();
            CurrentContextState?.LateUpdateStates();
        }

        #endregion

        #region State Transitions

        // These bypass the states' own transition checks. They are used to mirror
        // transitions that were already decided elsewhere (e.g. by the network owner).

        public void SwitchRootState(StateKey nextState)
        {
            EntityBaseState nextStateInstance = Factory.GetState(nextState);
            if (nextStateInstance == null) return;

            EntityBaseState previousState = CurrentState;

            CurrentState?.ExitStates(nextStateInstance);

            CurrentState = nextStateInstance;
            CurrentState.EnterState(previousState);
        }

        public void SwitchMovementState(StateKey nextSubState)
        {
            if (CurrentState == null) return;

            EntityBaseState previousSub = CurrentState.MovementSubState;
            EntityBaseState nextInstance = Factory.GetState(nextSubState);

            previousSub?.ExitStates(nextInstance);
            CurrentState.MovementSubState = nextInstance;
            nextInstance?.EnterState(previousSub);
        }

        public void SwitchActionState(StateKey nextSubState)
        {
            if (CurrentState == null) return;

            EntityBaseState previousSub = CurrentState.ActionSubState;
            EntityBaseState nextInstance = Factory.GetState(nextSubState);

            previousSub?.ExitStates(nextInstance);
            CurrentState.ActionSubState = nextInstance;
            nextInstance?.EnterState(previousSub);
        }

        /// <summary>
        /// Switches the active context state. Context lives at the machine level
        /// and persists across root state transitions automatically.
        /// </summary>
        public void SwitchContextState(StateKey contextState)
        {
            EntityBaseState newContext = Factory.GetState(contextState);
            if (newContext == null || newContext.StateType != EntityStateType.Context) return;
            if (newContext.StateKey == CurrentContextState?.StateKey) return;

            EntityBaseState previousContext = CurrentContextState;
            CurrentContextState?.ExitStates(newContext);

            CurrentContextState = newContext;

            InvokeContextStateTransitioned(contextState);
            CurrentContextState.EnterState(previousContext);
            CurrentContextState.InitializeSubStates();
        }

        public void ClearContextState()
        {
            CurrentContextState?.ExitStates(null);
            CurrentContextState = null;
        }

        #endregion
    }
}
