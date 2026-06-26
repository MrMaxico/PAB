using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems.Input
{
    public class LocalInputProvider : MonoBehaviour, IInputProvider
    {
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private bool _toggleRun;

        // Concrete state trackers handling their own logic
        private readonly MovementInputState _moveState = new();
        private readonly InputState<Vector2> _mouseState = new();
        private readonly ButtonInputState _jumpState = new();
        private readonly ButtonInputState _runState = new();
        private readonly ButtonInputState _shiftState = new();
        private readonly ButtonInputState _shootState = new();
        private readonly ButtonInputState _slideState = new();
        private readonly ButtonInputState _diveState = new();

        // Implementing the interface properties by exposing them as read-only
        public MovementInputState MoveState => _moveState;
        public IReadOnlyInputState<Vector2> MouseState => _mouseState;
        public IReadOnlyButtonState JumpState => _jumpState;
        public IReadOnlyButtonState RunState => _runState;
        public IReadOnlyButtonState ShiftState => _shiftState;
        public IReadOnlyButtonState ShootState => _shootState;
        public IReadOnlyButtonState SlideState => _slideState;
        public IReadOnlyButtonState DiveState => _diveState;


        public bool ToggleRun { get => _toggleRun; set => _toggleRun = value; }

        IReadOnlyMovementInputState IInputProvider.MovementState => MoveState;

        private void OnEnable()
        {
            RegisterInputActions(true);
        }

        private void OnDisable()
        {
            RegisterInputActions(false);

            _moveState.Reset();
            _mouseState.Reset();
            _jumpState.Reset();
            _runState.Reset();
            _shiftState.Reset();
            _shootState.Reset();
            _slideState.Reset();
            _diveState.Reset();
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

        #endregion
    }
}