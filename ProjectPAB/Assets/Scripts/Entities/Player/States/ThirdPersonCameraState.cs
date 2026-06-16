using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class ThirdPersonCameraState : ContextBaseState
    {
        private float _pitch;
        private float _yaw;
        private Vector3 _smoothVelocity;

        public float Pitch => _pitch;
        public float Yaw => _yaw;

        public ThirdPersonCameraState(PlayerStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory)
        {
            StateKey = PlayerStates.ThirdPersonCamera;
            StateType = PlayerStateType.Context;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} from {previousState?.StateKey.ToString() ?? "null"}");

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Inherit rotation from previous camera state to avoid snapping
            if (previousState is FirstPersonCameraState fps)
            {
                _pitch = fps.Pitch;
                _yaw = fps.Yaw;
            }
            else
            {
                // Initialize from current camera orientation
                Vector3 euler = Ctx.CameraHolder.eulerAngles;
                _yaw = euler.y;
                _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            }

            // Reset camera distance (child camera local position)
            float distance = Ctx.PlayerContext.CameraDistanceRange.y;
            Camera.main.transform.localPosition = new Vector3(0f, 0f, -distance);
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} to {nextState?.StateKey.ToString() ?? "null"}");
        }

        #region MonoBehaviours

        public override void UpdateState() { }

        public override void FixedUpdateState() { }

        public override void LateUpdateState()
        {
            IInputProvider input = Ctx.PlayerContext.InputProvider;
            if (input == null) return;

            float sensitivity = Ctx.PlayerContext.CameraSensitivity;
            Vector2 pitchClamp = Ctx.PlayerContext.CameraPitchClamp;

            // Accumulate rotation
            _yaw += input.MouseState.RawInputValue.x * sensitivity;
            _pitch -= input.MouseState.RawInputValue.y * sensitivity;
            _pitch = Mathf.Clamp(_pitch, pitchClamp.x, pitchClamp.y);

            // Apply rotation to camera holder
            Quaternion targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Ctx.CameraHolder.rotation = targetRotation;

            // Smoothly follow player position
            Vector3 targetPosition = Ctx.Transform.position;
            Ctx.CameraHolder.position = Vector3.SmoothDamp(
                Ctx.CameraHolder.position,
                targetPosition,
                ref _smoothVelocity,
                Ctx.PlayerContext.CameraSmoothTime
            );

            // Update orientation for movement direction (yaw only)
            Ctx.Orientation.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        #endregion

        #region Inputs

        #endregion

        public override void InitializeSubState() { }

        public override void CheckSwitchState() { }
    }
}