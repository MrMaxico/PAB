using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class WallWalkingState : MovementBaseState
    {
        public WallWalkingState(PlayerStateMachine currentContext, PlayerStateFactory charachterStateFactory) : base(currentContext, charachterStateFactory)
        {
            StateKey = PlayerStates.WallWalking;
        }

        /// <summary>Locked on enter — which direction along the wall we run.</summary>
        private Vector3 _runDirection;

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            // Lock the run direction based on which side of the wall we hit
            _runDirection = Ctx.WallDetector.WallForward;
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
        }

        #region MonoBehaveiours

        public override void FixedUpdateState()
        {
            // Gradually align run direction with the wall's current forward
            if (Ctx.WallDetector.WallForward != Vector3.zero)
            {
                _runDirection = Vector3.Slerp(_runDirection, Ctx.WallDetector.WallForward, Time.fixedDeltaTime * 10f);
            }

            HandleWallRunning();

            if (_runDirection != Vector3.zero)
            {
                Quaternion faceForward = Quaternion.LookRotation(_runDirection, Vector3.up);
                Ctx.PlayerObject.rotation = Quaternion.Slerp(Ctx.PlayerObject.rotation, faceForward, Time.fixedDeltaTime * 10f);
            }
        }

        public override void LateUpdateState() { }

        #endregion

        #region Inputs

        protected override void HandleInputAction(IInputProvider input)
        {
            if (Factory.HasState(PlayerStates.Jumping))
            {
                if (input.JumpState.UseBufferedPress())
                {
                    Vector3 forceAway = Ctx.WallDetector.WallNormal;
                    Vector3 forceUp = Vector3.up * 1.5f;
                    Vector3 forceForward = _runDirection * 0.2f;

                    Ctx.JumpDirection = (forceAway + forceUp + forceForward).normalized;

                    TrySwitchRootState(PlayerStates.Jumping);
                    return;
                }
            }
        }

        #endregion

        #region State Logic

        private void HandleWallRunning()
        {
            Vector3 targetVelocity = _runDirection * Ctx.PlayerContext.WallRunSpeed;
            Ctx.Rigidbody.linearVelocity = Vector3.Lerp(Ctx.Rigidbody.linearVelocity, targetVelocity, Time.fixedDeltaTime * 20f);
        }

        #endregion

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Climbing))
            {
                if (!Ctx.IsMovementInput && Ctx.WallDetector.HasAnyHit())
                {
                    TrySwitchState(PlayerStates.Climbing);
                    return;
                }
            }
        }
    }
}
