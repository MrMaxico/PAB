using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class FirstPersonCameraState : ContextBaseState
    {
        private float _pitch;
        private float _yaw;

        public float Pitch => _pitch;
        public float Yaw => _yaw;

        public FirstPersonCameraState(PlayerStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory)
        {
            StateKey = PlayerStates.FirstPersonCamera;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} from {previousState?.StateKey.ToString() ?? "null"}");

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Inherit rotation from third person to avoid snap
            if (previousState is ThirdPersonCameraState tps)
            {
                _pitch = tps.Pitch;
                _yaw = tps.Yaw;
            }
            else
            {
                Vector3 euler = Ctx.CameraHolder.eulerAngles;
                _yaw = euler.y;
                _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            }

            // Collapse camera to holder origin (no offset)
            Camera.main.transform.localPosition = Vector3.zero;

            // Immediately snap PlayerObject to camera yaw
            Ctx.PlayerObject.rotation = Quaternion.Euler(0f, _yaw, 0f);
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

            // Snap to head anchor position
            Ctx.CameraHolder.position = Ctx.FirstPersonAnchor.position;

            // Apply rotation
            Ctx.CameraHolder.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            // Sync orientation for movement
            Ctx.Orientation.rotation = Quaternion.Euler(0f, _yaw, 0f);

            // Lock PlayerObject to camera yaw — overrides whatever
            // WalkingState/RunningState set in FixedUpdate
            Ctx.PlayerObject.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        #endregion

        #region Inputs

        #endregion

        public override void InitializeSubState() { }

        public override void CheckSwitchState() { }
    }
}