using UnityEngine;

namespace Systems.Input
{
    public abstract class BaseInputProvider : MonoBehaviour, IInputProvider
    {
        protected readonly MovementInputState _moveState = new();
        protected readonly InputState<Vector2> _mouseState = new();
        protected readonly ButtonInputState _jumpState = new();
        protected readonly ButtonInputState _runState = new();
        protected readonly ButtonInputState _shiftState = new();
        protected readonly ButtonInputState _shootState = new();
        protected readonly ButtonInputState _slideState = new();
        protected readonly ButtonInputState _diveState = new();
        protected readonly ButtonInputState _switchPerspectiveState = new();

        public MovementInputState MoveState => _moveState;
        public IReadOnlyInputState<Vector2> MouseState => _mouseState;
        public IReadOnlyButtonState JumpState => _jumpState;
        public IReadOnlyButtonState RunState => _runState;
        public IReadOnlyButtonState ShiftState => _shiftState;
        public IReadOnlyButtonState ShootState => _shootState;
        public IReadOnlyButtonState SlideState => _slideState;
        public IReadOnlyButtonState DiveState => _diveState;
        public IReadOnlyButtonState SwitchPerspectiveState => _switchPerspectiveState;

        IReadOnlyMovementInputState IInputProvider.MovementState => MoveState;

        public ButtonInputFlags GetButtonFlags()
        {
            var flags = ButtonInputFlags.None;
            if (_jumpState.RawInputValue) flags |= ButtonInputFlags.Jump;
            if (_runState.RawInputValue) flags |= ButtonInputFlags.Run;
            if (_shiftState.RawInputValue) flags |= ButtonInputFlags.Shift;
            if (_shootState.RawInputValue) flags |= ButtonInputFlags.Shoot;
            if (_slideState.RawInputValue) flags |= ButtonInputFlags.Slide;
            if (_diveState.RawInputValue) flags |= ButtonInputFlags.Dive;
            if (_switchPerspectiveState.RawInputValue) flags |= ButtonInputFlags.SwitchPerspective;
            return flags;
        }

        protected void ResetAllStates()
        {
            _moveState.Reset();
            _mouseState.Reset();
            _jumpState.Reset();
            _runState.Reset();
            _shiftState.Reset();
            _shootState.Reset();
            _slideState.Reset();
            _diveState.Reset();
            _switchPerspectiveState.Reset();
        }
    }
}