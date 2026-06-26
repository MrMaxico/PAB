using UnityEngine;

namespace Systems.Input
{
    public interface IInputProvider
    {
        IReadOnlyMovementInputState MovementState { get; }
        IReadOnlyInputState<Vector2> MouseState { get; }
        IReadOnlyButtonState JumpState { get; }
        IReadOnlyButtonState RunState { get; }
        IReadOnlyButtonState ShiftState { get; }
        IReadOnlyButtonState ShootState { get; }
        IReadOnlyButtonState SlideState { get; }
        IReadOnlyButtonState DiveState { get; }
    }
}