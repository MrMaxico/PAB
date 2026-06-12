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

        public bool RawInputValue { get; private set; }
        public bool IsPressed { get; private set; }

        private int _pressedFrameId = -1;
        private int _releasedFrameId = -1;
        private int _doublePressedFrameId = -1;

        private float _bufferExpirationTime = -999f;
        private float _lastPressTime = -999f;

        public void UpdateValue(bool newValue)
        {
            bool previousButtonState = RawInputValue;
            RawInputValue = newValue;
            IsPressed = newValue;

            if (newValue && !previousButtonState)
            {
                _bufferExpirationTime = Time.time + BUFFER_TIME;
                _pressedFrameId = Time.frameCount;

                if (Time.time - _lastPressTime <= DOUBLE_PRESS_TIME)
                {
                    _doublePressedFrameId = Time.frameCount;
                }

                _lastPressTime = Time.time;
            }

            // Physical release up
            if (!newValue && previousButtonState)
            {
                _releasedFrameId = Time.frameCount;
            }
        }

        // ─── PURE INLINE TRIGGER CONSUMPTION METHODS ─── \\

        public bool OnPressed()
        {
            return Time.frameCount == _pressedFrameId;
        }

        public bool OnReleased()
        {
            return Time.frameCount == _releasedFrameId;
        }

        public bool UseDoublePress()
        {
            if (Time.frameCount == _doublePressedFrameId)
            {
                _doublePressedFrameId = -1;
                _pressedFrameId = -1;
                return true;
            }
            return false;
        }

        public bool UseBufferedPress()
        {
            if (Time.time <= _bufferExpirationTime)
            {
                _bufferExpirationTime = -999f;
                _pressedFrameId = -1;
                return true;
            }
            return false;
        }

        public bool UseBufferedPressOrHold()
        {
            if (Time.time <= _bufferExpirationTime || IsPressed)
            {
                _bufferExpirationTime = -999f;
                _pressedFrameId = -1;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            RawInputValue = false;
            IsPressed = false;
            _pressedFrameId = -1;
            _releasedFrameId = -1;
            _doublePressedFrameId = -1;
            _bufferExpirationTime = -999f;
            _lastPressTime = -999f;
        }
    }
}