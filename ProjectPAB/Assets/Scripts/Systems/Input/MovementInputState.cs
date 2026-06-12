using UnityEngine;

namespace Systems.Input
{
    [System.Flags]
    public enum MoveDirection
    {
        None = 0,
        Forward = 1 << 0, // Binary: 00000001 (Decimal: 1)
        Backward = 1 << 1, // Binary: 00000010 (Decimal: 2)
        Left = 1 << 2, // Binary: 00000100 (Decimal: 4)
        Right = 1 << 3, // Binary: 00001000 (Decimal: 8)

        // Diagonals are automatically created by combining the bits!
        ForwardLeft = Forward | Left,   // Binary: 00000101 (Decimal: 5)
        ForwardRight = Forward | Right,  // Binary: 00001001 (Decimal: 9)
        BackwardLeft = Backward | Left,  // Binary: 00000110 (Decimal: 6)
        BackwardRight = Backward | Right  // Binary: 00001010 (Decimal: 10)
    }

    public class MovementInputState : InputState<Vector2>, IReadOnlyMovementInputState
    {
        private const float PRESS_THRESHOLD = 0.7f;
        private const float RELEASE_THRESHOLD = 0.3f;
        private const float DOUBLE_TAP_WINDOW = 0.25f;

        private MoveDirection _currentDirectionHold = MoveDirection.None;
        private MoveDirection _lastPressedDirection = MoveDirection.None;
        private MoveDirection _doubleTappedDirection = MoveDirection.None;

        private float _lastPressTime = -999f;

        public override void UpdateValue(Vector2 newValue)
        {
            base.UpdateValue(newValue);
            IsPressed = newValue.magnitude > PRESS_THRESHOLD;

            MoveDirection previousHold = _currentDirectionHold;
            MoveDirection rawDirections = MoveDirection.None;

            // 1. Build the bitmask dynamically based on raw axes thresholds
            if (newValue.y > RELEASE_THRESHOLD) rawDirections |= MoveDirection.Forward;
            if (newValue.y < -RELEASE_THRESHOLD) rawDirections |= MoveDirection.Backward;
            if (newValue.x > RELEASE_THRESHOLD) rawDirections |= MoveDirection.Right;
            if (newValue.x < -RELEASE_THRESHOLD) rawDirections |= MoveDirection.Left;

            // Apply hysteresis check to fully commit to a registered direction change
            if (newValue.magnitude > PRESS_THRESHOLD)
            {
                _currentDirectionHold = rawDirections;
            }
            else if (newValue.magnitude < RELEASE_THRESHOLD)
            {
                _currentDirectionHold = MoveDirection.None;
                _doubleTappedDirection = MoveDirection.None;
            }

            // 2. Evaluate if a brand-new direction flag was just pressed this frame
            if (_currentDirectionHold != MoveDirection.None && _currentDirectionHold != previousHold)
            {
                // Check if it matches our last pressed direction within the time window
                if (_currentDirectionHold == _lastPressedDirection && (Time.time - _lastPressTime <= DOUBLE_TAP_WINDOW))
                {
                    _doubleTappedDirection = _currentDirectionHold;
                }
                else
                {
                    _doubleTappedDirection = MoveDirection.None;
                }

                _lastPressedDirection = _currentDirectionHold;
                _lastPressTime = Time.time;
            }
        }

        public bool UseDoubleTap(MoveDirection direction)
        {
            // Use the bitwise HasFlag check to support checking partial elements or exact diagonals
            if (_doubleTappedDirection != MoveDirection.None && _doubleTappedDirection.HasFlag(direction))
            {
                _doubleTappedDirection = MoveDirection.None; // Consume the event gesture
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            _currentDirectionHold = MoveDirection.None;
            _lastPressedDirection = MoveDirection.None;
            _doubleTappedDirection = MoveDirection.None;
            _lastPressTime = -999f;
        }
    }
}