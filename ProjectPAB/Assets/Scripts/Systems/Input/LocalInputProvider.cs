using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems.Input
{
    public class LocalInputProvider : MonoBehaviour, IInputProvider
    {
        [SerializeField] private PlayerInput _playerInput;
        [Header("")]
        [SerializeField] private Vector2 _moveInput;
        [SerializeField] private bool _jumpInput;
        [SerializeField] private bool _runInput;
        [SerializeField] private Vector2 _mouseInput;
        [SerializeField] private bool _shiftInput;
        [SerializeField] private bool _shootInput;

        [SerializeField] private bool _toggleRun;
        public bool ToggleRun { get => _toggleRun; set => _toggleRun = value; }

        private void OnEnable()
        {
            var inputActions = _playerInput.actions;

            var moveAction = inputActions["Move"];
            moveAction.started += OnMoveInput;
            moveAction.performed += OnMoveInput;
            moveAction.canceled += OnMoveInput;

            var jumpAction = inputActions["Jump"];
            jumpAction.started += OnJumpInput;
            jumpAction.performed += OnJumpInput;
            jumpAction.canceled += OnJumpInput;

            var runAction = inputActions["Run"];
            runAction.started += OnRunInput;
            runAction.performed += OnRunInput;
            runAction.canceled += OnRunInput;

            var cameraAction = inputActions["Camera"];
            cameraAction.started += OnCameraInput;
            cameraAction.performed += OnCameraInput;
            cameraAction.canceled += OnCameraInput;

            var shiftAction = inputActions["Shift"];
            shiftAction.started += OnShiftInput;
            shiftAction.performed += OnShiftInput;
            shiftAction.canceled += OnShiftInput;

            var shootAction = inputActions["Shoot"];
            shootAction.started += OnShootInput;
            shootAction.performed += OnShootInput;
            shootAction.canceled += OnShootInput;
        }

        private void OnDisable()
        {
            var inputActions = _playerInput.actions;

            var moveAction = inputActions["Move"];
            moveAction.started -= OnMoveInput;
            moveAction.performed -= OnMoveInput;
            moveAction.canceled -= OnMoveInput;

            var jumpAction = inputActions["Jump"];
            jumpAction.started -= OnJumpInput;
            jumpAction.performed -= OnJumpInput;
            jumpAction.canceled -= OnJumpInput;

            var runAction = inputActions["Run"];
            runAction.started -= OnRunInput;
            runAction.performed -= OnRunInput;
            runAction.canceled -= OnRunInput;

            var cameraAction = inputActions["Camera"];
            cameraAction.started -= OnCameraInput;
            cameraAction.performed -= OnCameraInput;
            cameraAction.canceled -= OnCameraInput;

            var shiftAction = inputActions["Shift"];
            shiftAction.started -= OnShiftInput;
            shiftAction.performed -= OnShiftInput;
            shiftAction.canceled -= OnShiftInput;

            var shootAction = inputActions["Shoot"];
            shootAction.started -= OnShootInput;
            shootAction.performed -= OnShootInput;
            shootAction.canceled -= OnShootInput;

            _moveInput = Vector2.zero;
            _runInput = false;
            _jumpInput = false;
            _shiftInput = false;
            _shootInput = false;
        }

        private void OnMoveInput(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnJumpInput(InputAction.CallbackContext context)
        {
            _jumpInput = context.ReadValueAsButton();
        }

        private void OnRunInput(InputAction.CallbackContext context)
        {
            if (_toggleRun)
            {
                if (context.started) _runInput = !_runInput;
            }
            else
            {
                _runInput = context.ReadValueAsButton();
            }
        }

        private void OnCameraInput(InputAction.CallbackContext context)
        {
            _mouseInput = context.ReadValue<Vector2>();
        }

        private void OnShiftInput(InputAction.CallbackContext context)
        {
            _shiftInput = context.ReadValueAsButton();
        }

        private void OnShootInput(InputAction.CallbackContext context)
        {
            _shootInput = context.ReadValueAsButton();
        }

        public Vector2 GetMoveInput() => _moveInput;

        public bool GetJumpInput() => _jumpInput;

        public bool GetRunInput() => _runInput;

        public Vector2 GetMouseInput() => _mouseInput;

        public bool GetShiftInput() => _shiftInput;

        public bool GetShootInput() => _shootInput;
    }
}