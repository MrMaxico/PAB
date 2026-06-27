using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems.Input
{
    public class LocalInputProvider : BaseInputProvider
    {
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private bool _toggleRun;

        public bool ToggleRun { get => _toggleRun; set => _toggleRun = value; }

        private void OnEnable()
        {
            RegisterInputActions(true);
        }

        private void OnDisable()
        {
            RegisterInputActions(false);
            ResetAllStates();
        }

        private void RegisterInputActions(bool subscribe)
        {
            if (_playerInput == null || _playerInput.actions == null) return;

            var inputActions = _playerInput.actions;

            ToggleActionBinding(inputActions["Move"], OnMoveInput, subscribe);
            ToggleActionBinding(inputActions["Jump"], OnJumpInput, subscribe);
            ToggleActionBinding(inputActions["Run"], OnRunInput, subscribe);
            ToggleActionBinding(inputActions["Camera"], OnCameraInput, subscribe);
            ToggleActionBinding(inputActions["Shift"], OnShiftInput, subscribe);
            ToggleActionBinding(inputActions["Shoot"], OnShootInput, subscribe);
            ToggleActionBinding(inputActions["Slide"], OnSlideInput, subscribe);
            ToggleActionBinding(inputActions["Dive"], OnDiveInput, subscribe);
            ToggleActionBinding(inputActions["SwitchPerspective"], OnSwitchPerspectiveInput, subscribe);
        }

        private void ToggleActionBinding(InputAction action, System.Action<InputAction.CallbackContext> callback, bool subscribe)
        {
            if (action == null) return;

            if (subscribe)
            {
                action.started += callback;
                action.performed += callback;
                action.canceled += callback;
            }
            else
            {
                action.started -= callback;
                action.performed -= callback;
                action.canceled -= callback;
            }
        }

        #region Input System Callbacks

        private void OnMoveInput(InputAction.CallbackContext context)
        {
            _moveState.UpdateValue(context.ReadValue<Vector2>());
        }

        private void OnJumpInput(InputAction.CallbackContext context)
        {
            _jumpState.UpdateValue(context.ReadValueAsButton());
        }

        private void OnRunInput(InputAction.CallbackContext context)
        {
            if (_toggleRun)
            {
                if (context.started)
                {
                    _runState.UpdateValue(!_runState.IsPressed);
                }
            }
            else
            {
                _runState.UpdateValue(context.ReadValueAsButton());
            }
        }

        private void OnCameraInput(InputAction.CallbackContext context)
        {
            _mouseState.UpdateValue(context.ReadValue<Vector2>());
        }

        private void OnShiftInput(InputAction.CallbackContext context)
        {
            _shiftState.UpdateValue(context.ReadValueAsButton());
        }

        private void OnShootInput(InputAction.CallbackContext context)
        {
            _shootState.UpdateValue(context.ReadValueAsButton());
        }

        private void OnSlideInput(InputAction.CallbackContext context)
        {
            _slideState.UpdateValue(context.ReadValueAsButton());
        }

        private void OnDiveInput(InputAction.CallbackContext context)
        {
            _diveState.UpdateValue(context.ReadValueAsButton());
        }

        private void OnSwitchPerspectiveInput(InputAction.CallbackContext context)
        {
            _switchPerspectiveState.UpdateValue(context.ReadValueAsButton());
        }

        #endregion
    }
}