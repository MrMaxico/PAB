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

        private Vector3 _runDirection;

        public override void EnterState(PlayerBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.WallDetector.Tick();

            _runDirection = Ctx.WallDetector.Hit.Forward;
        }

        public override void ExitState(PlayerBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif
        }

        #region MonoBehaveiours

        public override void FixedUpdateState()
        {
            if (Ctx.WallDetector.Hit.Forward != Vector3.zero)
            {
                _runDirection = Vector3.Slerp(_runDirection, Ctx.WallDetector.Hit.Forward, Time.fixedDeltaTime * 10f);
            }

            HandleWallRunning();

            if (_runDirection != Vector3.zero)
            {
                Quaternion faceForward = Quaternion.LookRotation(_runDirection, Vector3.up);
                Ctx.SmoothModelRotation = Quaternion.Slerp(Ctx.PlayerModel.rotation, faceForward, Time.fixedDeltaTime * 10f);
            }
        }

        #endregion

        #region Inputs

        protected override void HandleInputAction(IInputProvider input)
        {
            if (Factory.HasState(PlayerStates.Jumping))
            {
                if (input.JumpState.UseBufferedPress())
                {
                    Vector3 forceAway = Ctx.WallDetector.Hit.Normal;
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
            Vector3 targetVelocity = _runDirection * Ctx.PlayerContext.WallRunSpeed + Ctx.PlatformVelocity;
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
