using Entities.Base.States.Base;
using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class SwingingState : PlayerMovementState
    {
        private const float JumpOffMinTangentialSpeed = 1.25f;

        private float _angle;
        private float _angularVelocity;

        private Vector2 _currentInput;
        private Vector3 _releaseVelocity;

        private float _facingSign = 1f;
        private Vector3 _bottomTangent;

        public SwingingState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Swinging;
        }

        public override void EnterState(EntityBaseState previousState = null)
        {
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            if (CurrentSuperState is BarredState barred)
            {
                _bottomTangent = barred.BottomTangent;
                _angle = barred.EntryAngle;
                _angularVelocity = barred.EntryAngularVelocity;
                _facingSign = barred.FacingSign >= 0f ? 1f : -1f;
            }
            else
            {
                float length = Mathf.Max(0.5f, Ctx.PlayerContext.SwingLength);
                Vector3 radial = (Ctx.Rigidbody.position - Ctx.BarPivot).normalized;
                Vector3 tangent = Vector3.Cross(Ctx.BarAxis, radial).normalized;

                _bottomTangent = Vector3.Cross(Ctx.BarAxis, Vector3.down).normalized;
                if (_bottomTangent.sqrMagnitude < 0.0001f)
                    _bottomTangent = Vector3.forward;

                _angle = Vector3.SignedAngle(Vector3.down, radial, Ctx.BarAxis) * Mathf.Deg2Rad;
                _angularVelocity = Vector3.Dot(Ctx.Rigidbody.linearVelocity, tangent) / length;
                _facingSign = 1f;
            }

            _releaseVelocity = Vector3.zero;
            Ctx.Rigidbody.useGravity = false;
        }

        public override void ExitState(EntityBaseState nextState = null)
        {
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");

            Ctx.Rigidbody.useGravity = true;
            Ctx.Rigidbody.linearVelocity = _releaseVelocity;
        }

        public override void FixedUpdateState()
        {
            float length = Mathf.Max(0.5f, Ctx.PlayerContext.SwingLength);

            Vector3 radial = Quaternion.AngleAxis(_angle * Mathf.Rad2Deg, Ctx.BarAxis) * Vector3.down;
            Vector3 tangent = Vector3.Cross(Ctx.BarAxis, radial).normalized;

            float tangentIntent = GetTangentIntent();

            float gravityTerm = -(Ctx.PlayerContext.SwingGravity / length) * Mathf.Sin(_angle);
            float pumpTerm = tangentIntent * Ctx.PlayerContext.SwingPumpStrength;

            _angularVelocity += (gravityTerm + pumpTerm) * Time.fixedDeltaTime;
            _angularVelocity *= Mathf.Clamp01(1f - Ctx.PlayerContext.SwingDamping * Time.fixedDeltaTime);
            _angle += _angularVelocity * Time.fixedDeltaTime;

            radial = Quaternion.AngleAxis(_angle * Mathf.Rad2Deg, Ctx.BarAxis) * Vector3.down;
            tangent = Vector3.Cross(Ctx.BarAxis, radial).normalized;

            float tangentialSpeed = _angularVelocity * length;
            Vector3 desiredTangentialVelocity = tangent * tangentialSpeed;

            Vector3 toPivot = Ctx.BarPivot - Ctx.Rigidbody.position;
            float currentLength = toPivot.magnitude;
            Vector3 radialDir = currentLength > 0.0001f ? toPivot / currentLength : -radial;
            float lengthError = currentLength - length;
            Vector3 radialCorrectionVelocity = radialDir * (lengthError * 14f);

            Vector3 targetVelocity = desiredTangentialVelocity + radialCorrectionVelocity;
            Ctx.Rigidbody.linearVelocity = Vector3.Lerp(
                Ctx.Rigidbody.linearVelocity,
                targetVelocity,
                Time.fixedDeltaTime * 20f);

            _releaseVelocity = Ctx.Rigidbody.linearVelocity;

            if (tangent.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(tangent * _facingSign, -radial);
                Ctx.SmoothModelRotation = Quaternion.Slerp(Ctx.PlayerModel.rotation, targetRotation, Time.fixedDeltaTime * 15f);
            }
        }

        private Vector3 GetCameraRelativeMove(Vector2 input)
        {
            Vector3 move = (Ctx.Orientation.forward * input.y) + (Ctx.Orientation.right * input.x);
            move = Vector3.ProjectOnPlane(move, Vector3.up);

            return move.sqrMagnitude > 0.0001f ? move.normalized : Vector3.zero;
        }

        private float GetTangentIntent()
        {
            Vector3 wish = GetCameraRelativeMove(_currentInput);
            float intent = Vector3.Dot(wish, _bottomTangent);
            return Mathf.Abs(intent) > 0.05f ? intent : 0f;
        }

        protected override void HandleInputAction(IInputProvider input)
        {
            _currentInput = input.MovementState.RawInputValue;

            if (!input.JumpState.OnPressed())
                return;

            float length = Mathf.Max(0.5f, Ctx.PlayerContext.SwingLength);
            float tangentialSpeed = Mathf.Abs(_angularVelocity) * length;

            if (tangentialSpeed < JumpOffMinTangentialSpeed)
            {
                if (Factory.HasState(PlayerStates.Airborne) && TrySwitchRootState(PlayerStates.Airborne))
                {
                    Ctx.BarGate.SuppressFor(Ctx.PlayerContext.SameBarJumpGrace);
                    return;
                }
            }
            else
            {
                if (Factory.HasState(PlayerStates.Jumping) && TrySwitchRootState(PlayerStates.Jumping))
                {
                    Ctx.BarGate.SuppressFor(Ctx.PlayerContext.SameBarJumpGrace);
                    return;
                }
            }
        }
    }
}