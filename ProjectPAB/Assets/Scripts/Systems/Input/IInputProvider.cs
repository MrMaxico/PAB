using UnityEngine;

namespace Systems.Input
{
    public interface IInputProvider
    {
        Vector2 GetMoveInput();
        bool GetJumpInput();
        bool GetRunInput();
        Vector2 GetMouseInput();
        bool GetShiftInput();
        bool GetShootInput();
    }
}