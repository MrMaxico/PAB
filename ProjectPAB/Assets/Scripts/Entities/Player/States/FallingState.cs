using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class FallingState : MovementBaseState
    {
        private float _timeSinceLastFalling = -999f;

        private float _entryHorizontalSpeed;

        private float _currentMaxStrafeSpeed;

        public FallingState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Falling;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif
            Vector3 vel = Ctx.Rigidbody.linearVelocity;
            _entryHorizontalSpeed = new Vector3(vel.x, 0, vel.z).magnitude;

            if (Time.time - _timeSinceLastFalling < Ctx.PlayerContext.StrafeHopWindow)
            {
                // A fresh ground jump grows the cap; a mid-air re-entry (Jumping -> Airborne
                // handoff, Idling -> Falling) keeps it untouched.
                if (Ctx.AirStrafeSpeedBoost)
                {
                    _currentMaxStrafeSpeed = Mathf.Min(
                        _currentMaxStrafeSpeed + Ctx.PlayerContext.AirStrafeGain,
                        Ctx.PlayerContext.MaxAirStrafeSpeed);
                }
            }
            else
            {
                // Chain broken: too long since we were last airborne.
                _currentMaxStrafeSpeed = Ctx.PlayerContext.BaseAirStrafeSpeed;
            }
        }

        public override void ExitState(PlayerBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.AirStrafeSpeedBoost = false;

            _timeSinceLastFalling = Time.time;
        }

        #region Monobehaviours

        public override void FixedUpdateState()
        {
            HandleStrafing();
        }

        #endregion

        #region State Logic

        private void HandleStrafing()
        {
            Vector3 strafeDir = (Ctx.Orientation.forward * _moveInput.y) + (Ctx.Orientation.right * _moveInput.x);
            strafeDir.y = 0f;
            if (strafeDir.sqrMagnitude > 1f) strafeDir.Normalize();

            Vector3 velocity = Ctx.Rigidbody.linearVelocity;
            Vector3 horizontalVelocity = new(velocity.x, 0f, velocity.z);

            horizontalVelocity += strafeDir * (Ctx.PlayerContext.AirStrafeForce * Time.fixedDeltaTime * 10f);

            float maxSpeed = Mathf.Max(_entryHorizontalSpeed, _currentMaxStrafeSpeed);
            if (horizontalVelocity.magnitude > maxSpeed) horizontalVelocity = horizontalVelocity.normalized * maxSpeed;

            Ctx.Rigidbody.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
        }

        #endregion

        #region Inputs

        private Vector3 _moveInput;

        protected override void HandleInputAction(IInputProvider input)
        {
            _moveInput = input.MovementState.RawInputValue;
        }

        #endregion
    }
}