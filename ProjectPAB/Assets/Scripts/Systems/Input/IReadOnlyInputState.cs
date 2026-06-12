namespace Systems.Input
{
    // For general values (like Vector2)
    public interface IReadOnlyInputState<out T>
    {
        T RawInputValue { get; }
        bool IsPressed { get; }
    }

    // For buttons
    public interface IReadOnlyButtonState
    {
        bool RawInputValue { get; }
        bool IsPressed { get; }
        bool WasPressedThisFrame { get; }
        bool WasReleasedThisFrame { get; }
        bool UseBufferedPress();
        bool UseBufferedPressAndHold();
        bool UseDoublePress();
    }

    public interface IReadOnlyMovementInputState : IReadOnlyInputState<UnityEngine.Vector2>
    {
        bool UseDoubleTap(MoveDirection direction);
    }
}