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
        private const float BUFFER_TIME = 0.25f;
        private const float DOUBLE_PRESS_TIME = 0.25f;

        public bool RawInputValue { get; private set; }
        public bool IsPressed { get; private set; }
        public bool WasPressedThisFrame { get; private set; }
        public bool WasReleasedThisFrame { get; private set; }
        public bool IsDoublePressed { get; private set; }

        private float _bufferExpirationTime = -999f;
        private float _lastPressTime = -999f;

        public void UpdateValue(bool newValue)
        {
            bool previousButtonState = RawInputValue;
            RawInputValue = newValue;
            IsPressed = newValue;

            WasPressedThisFrame = newValue && !previousButtonState;
            WasReleasedThisFrame = !newValue && previousButtonState;

            if (WasPressedThisFrame)
            {
                if (Time.time - _lastPressTime <= DOUBLE_PRESS_TIME)
                {
                    IsDoublePressed = true;
                }
                else
                {
                    IsDoublePressed = false;
                }

                _lastPressTime = Time.time;

                _bufferExpirationTime = Time.time + BUFFER_TIME;
            }

            if (WasReleasedThisFrame)
            {
                IsDoublePressed = false;
            }
        }

        public bool UseBufferedPress()
        {
            if (Time.time <= _bufferExpirationTime)
            {
                _bufferExpirationTime = -999f;
                return true;
            }
            return false;
        }

        public bool UseBufferedPressAndHold()
        {
            if (Time.time <= _bufferExpirationTime)
            {
                _bufferExpirationTime = -999f;
                return true;
            }

            if (IsPressed)
            {
                _bufferExpirationTime = -999f;
                return true;
            }

            return false;
        }

        public bool UseDoublePress()
        {
            if (IsDoublePressed)
            {
                IsDoublePressed = false;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            RawInputValue = false;
            IsPressed = false;
            WasPressedThisFrame = false;
            WasReleasedThisFrame = false;
            IsDoublePressed = false;
            _bufferExpirationTime = -999f;
            _lastPressTime = -999f;
        }
    }
}