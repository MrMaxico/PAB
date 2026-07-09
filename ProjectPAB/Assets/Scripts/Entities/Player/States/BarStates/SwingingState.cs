using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class SwingingState : MovementBaseState
    {
        private float _theta;   // angle from hanging straight down, radians
        private float _omega;   // angular velocity, rad/s

        private float _armLength;
        private float _pumpInput;

        private BarredState _bar;

        public SwingingState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Swinging;
        }

        public override void EnterState(PlayerBaseState previousState = null)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif
            _bar = CurrentSuperState as BarredState;
            if (_bar == null || !_bar.HasBar) return;

            _armLength = Ctx.PlayerContext.BarArmLength;

            _theta = MeasureAngle(Ctx.transform.position - _bar.BarPivot);
            _omega = Vector3.Dot(_bar.EntryVelocity, Tangent(_theta)) / _armLength;

            Ctx.transform.position = _bar.BarPivot + Arm(_theta) * _armLength;
        }

        public override void ExitState(PlayerBaseState nextState = null)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif
        }

        #region MonoBehaviours

        public override void FixedUpdateState()
        {
            if (_bar == null || !_bar.HasBar) return;

            float g = Physics.gravity.magnitude;
            float L = _armLength;
            float dt = Time.fixedDeltaTime;

            // Gravity restoring + player pumping - damping.
            float alpha = -(g / L) * Mathf.Sin(_theta);
            alpha += Ctx.PlayerContext.BarPumpAcceleration * _pumpInput;
            alpha -= Ctx.PlayerContext.BarSwingDamping * _omega;

            // Semi-implicit Euler.
            _omega += alpha * dt;
            _omega = Mathf.Clamp(_omega, -Ctx.PlayerContext.BarMaxAngularSpeed, Ctx.PlayerContext.BarMaxAngularSpeed);
            _theta += _omega * dt;

            Vector3 target = _bar.BarPivot + Arm(_theta) * L;
            Ctx.Rigidbody.MovePosition(target);

            // Face the direction of travel along the arc.
            Vector3 look = Tangent(_theta) * Mathf.Sign(_omega == 0f ? 1f : _omega);
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(look.normalized, Vector3.up);
                Ctx.SmoothModelRotation = Quaternion.Slerp(Ctx.PlayerModel.rotation, targetRot, 15f * dt);
            }
        }

        #endregion

        #region State Logic

        // Arm direction at a given angle: PlaneDown at theta = 0, rotating toward PlaneTangent.
        private Vector3 Arm(float theta) =>
            _bar.PlaneDown * Mathf.Cos(theta) + _bar.PlaneTangent * Mathf.Sin(theta);

        // Direction of travel along the arc (derivative of Arm).
        private Vector3 Tangent(float theta) =>
            -_bar.PlaneDown * Mathf.Sin(theta) + _bar.PlaneTangent * Mathf.Cos(theta);

        // World arm vector (pivot -> player) to an angle in the swing plane.
        private float MeasureAngle(Vector3 arm)
        {
            arm = Vector3.ProjectOnPlane(arm, _bar.BarAxis);
            if (arm.sqrMagnitude < 0.0001f) return 0f;
            arm.Normalize();

            float cos = Vector3.Dot(arm, _bar.PlaneDown);
            float sin = Vector3.Dot(arm, _bar.PlaneTangent);
            return Mathf.Atan2(sin, cos);
        }

        #endregion

        #region Inputs

        protected override void HandleInputAction(IInputProvider input)
        {
            _pumpInput = input.MovementState.RawInputValue.y;

            if (Factory.HasState(PlayerStates.Jumping) && input.JumpState.UseBufferedPressOrHold())
            {
                LaunchOff();
            }
        }

        #endregion

        #region State Switching

        private void LaunchOff()
        {
            Vector3 travel = Tangent(_theta) * Mathf.Sign(_omega == 0f ? 1f : _omega);
            travel.y = 0f;
            travel = travel.normalized;

            float speed01 = Mathf.Clamp01(Mathf.Abs(_omega) / Ctx.PlayerContext.BarMaxAngularSpeed);
            float horizontal = Mathf.Lerp(0.4f, 1.2f, speed01);

            Ctx.JumpDirection = travel * horizontal + Vector3.up * Ctx.PlayerContext.BarLaunchBoost;
            TrySwitchState(PlayerStates.Jumping);
        }

        #endregion
    }
}
