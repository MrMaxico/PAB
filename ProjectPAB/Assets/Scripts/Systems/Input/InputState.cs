using UnityEngine;

namespace Systems.Input
{
    public class InputState<T> : IReadOnlyInputState<T>
    {
        public T RawInputValue { get; protected set; }
        public bool IsPressed { get; protected set; }

        public virtual void UpdateValue(T newValue)
        {
            RawInputValue = newValue;

            if (newValue is Vector2 vectorValue)
            {
                IsPressed = vectorValue.sqrMagnitude > 0.01f;
            }
        }

        public virtual void Reset()
        {
            RawInputValue = default;
            IsPressed = false;
        }
    }

    public class ButtonInputState : IReadOnlyButtonState
    {
        private const float BUFFER_TIME = 0.15f;
        private const float DOUBLE_PRESS_TIME = 0.25f;

        // Matches your exact property name!
        public bool RawInputValue { get; private set; }
        public bool IsPressed { get; private set; }

        // Hidden event triggers for your single-call methods
        private bool _wasPressedEventFired;
        private bool _wasReleasedEventFired;
        private bool _isDoublePressedEventFired;

        private float _bufferExpirationTime = -999f;
        private float _lastPressTime = -999f;

        public void UpdateValue(bool newValue)
        {
            bool previousButtonState = RawInputValue;
            RawInputValue = newValue;
            IsPressed = newValue;

            // Physical button pressed down event
            if (newValue && !previousButtonState)
            {
                _bufferExpirationTime = Time.time + BUFFER_TIME;
                _wasPressedEventFired = true;

                // Check for double press tracking
                if (Time.time - _lastPressTime <= DOUBLE_PRESS_TIME)
                {
                    _isDoublePressedEventFired = true;
                }

                _lastPressTime = Time.time;
            }

            // Physical button released up event
            if (!newValue && previousButtonState)
            {
                _wasReleasedEventFired = true;
            }
        }

        // ─── PURE INLINE TRIGGER CONSUMPTION METHODS ─── \\

        public bool OnPressed()
        {
            if (_wasPressedEventFired)
            {
                _wasPressedEventFired = false; // Consume instantly
                return true;
            }
            return false;
        }

        public bool OnReleased()
        {
            if (_wasReleasedEventFired)
            {
                _wasReleasedEventFired = false; // Consume instantly
                return true;
            }
            return false;
        }

        public bool UseDoublePress()
        {
            if (_isDoublePressedEventFired)
            {
                _isDoublePressedEventFired = false; // Consume instantly
                _wasPressedEventFired = false;      // Prevent normal press from firing alongside it
                return true;
            }
            return false;
        }

        public bool UseBufferedPress()
        {
            if (Time.time <= _bufferExpirationTime)
            {
                _bufferExpirationTime = -999f;
                _wasPressedEventFired = false; // Synchronize consumption 
                return true;
            }
            return false;
        }

        public bool UseBufferedPressOrHold()
        {
            if (Time.time <= _bufferExpirationTime || IsPressed)
            {
                _bufferExpirationTime = -999f;
                _wasPressedEventFired = false; // Synchronize consumption
                return true;
            }
            return false;
        }

        public void Reset()
        {
            RawInputValue = false;
            IsPressed = false;
            _wasPressedEventFired = false;
            _wasReleasedEventFired = false;
            _isDoublePressedEventFired = false;
            _bufferExpirationTime = -999f;
            _lastPressTime = -999f;
        }
    }
}