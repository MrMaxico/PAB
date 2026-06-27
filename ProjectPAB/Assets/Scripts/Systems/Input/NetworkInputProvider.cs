using Unity.Netcode;
using UnityEngine;

namespace Systems.Input
{
    public class NetworkInputProvider : BaseInputProvider
    {
        public Quaternion LastModelRotation { get; private set; } = Quaternion.identity;
        public bool LastSnapRotation { get; private set; }

        public void UpdateNetworkInputs(NetworkedInputs input)
        {
            _moveState.UpdateValue(input.Move);
            _mouseState.UpdateValue(input.Mouse);
            _jumpState.UpdateValue((input.Buttons & ButtonInputFlags.Jump) != 0);
            _runState.UpdateValue((input.Buttons & ButtonInputFlags.Run) != 0);
            _shiftState.UpdateValue((input.Buttons & ButtonInputFlags.Shift) != 0);
            _shootState.UpdateValue((input.Buttons & ButtonInputFlags.Shoot) != 0);
            _slideState.UpdateValue((input.Buttons & ButtonInputFlags.Slide) != 0);
            _diveState.UpdateValue((input.Buttons & ButtonInputFlags.Dive) != 0);
            _switchPerspectiveState.UpdateValue((input.Buttons & ButtonInputFlags.SwitchPerspective) != 0);
            LastModelRotation = input.ModelRotation;
            LastSnapRotation = input.SnapRotation;
        }
    }

    [System.Flags]
    public enum ButtonInputFlags : byte
    {
        None = 0,
        Jump = 1 << 0,
        Run = 1 << 1,
        Shift = 1 << 2,
        Shoot = 1 << 3,
        Slide = 1 << 4,
        Dive = 1 << 5,
        SwitchPerspective = 1 << 6,
    }

    public struct NetworkedInputs : INetworkSerializeByMemcpy
    {
        public Vector2 Move;
        public Vector2 Mouse;
        public ButtonInputFlags Buttons;
        public Quaternion ModelRotation;
        public bool SnapRotation;
    }
}
