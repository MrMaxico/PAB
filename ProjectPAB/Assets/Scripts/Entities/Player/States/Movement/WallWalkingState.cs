using Entities.Base.States.Base;
using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class WallWalkingState : PlayerMovementState
    {
        public WallWalkingState(PlayerStateMachine currentContext, PlayerStateFactory charachterStateFactory) : base(currentContext, charachterStateFactory)
        {
            StateKey = PlayerStates.WallWalking;
        }

        private Vector3 _runDirection;

        public override void EnterState(EntityBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif

            _runDirection = Ctx.Walls.TryGetHit(out RaycastHit wall) ? Ctx.WallForward(wall) : Vector3.zero;
        }

        public override void ExitState(EntityBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif
        }

        #region MonoBehaveiours

        public override void FixedUpdateState()
        {
            if (Ctx.Walls.TryGetHit(out RaycastHit wall))
            {
                _runDirection = Vector3.Slerp(_runDirection, Ctx.WallForward(wall), Time.fixedDeltaTime * 10f);
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
                    Vector3 forceAway = Ctx.Walls.Hit.normal;
                    Vector3 forceUp = Vector3.up * 1.5f;
                    Vector3 forceForward = _runDirection * 0.2f;

                    Ctx.JumpDirection = (forceAway + forceUp + forceForward).normalized;

                    TrySwitchRootState(PlayerStates.Jumping);
                    Ctx.WallGate.SuppressFor(Ctx.PlayerContext.WallJumpGrace);
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
                if (!Ctx.IsMovementInput && Ctx.Walls.IsHit)
                {
                    TrySwitchState(PlayerStates.Climbing);
                    return;
                }
            }
        }
    }
}
