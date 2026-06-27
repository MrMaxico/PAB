using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class SlidingState : MovementBaseState
    {
        private float currentSlideSpeed;

        public SlidingState(PlayerStateMachine currentContext, PlayerStateFactory charachterStateFactory) : base(currentContext, charachterStateFactory)
        {
            StateKey = PlayerStates.Sliding;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif
        }

        public override void ExitState(PlayerBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif
        }

        #region MonoBehaveiours

        public override void UpdateState()
        {
            if (Ctx.GroundDetector.IsSloped && Ctx.MoveDirection.y < 0)
            {
                if (currentSlideSpeed < Ctx.PlayerContext.MaxSlideSpeed)
                {
                    currentSlideSpeed += Time.fixedDeltaTime * Ctx.PlayerContext.SlideAcceleration;
                }
                else
                {
                    currentSlideSpeed = Ctx.PlayerContext.MaxSlideSpeed;
                }
            }
            else if (!Ctx.GroundDetector.IsSloped)
            {
                currentSlideSpeed = Ctx.PlayerContext.BaseSlideSpeed;
            }
        }

        public override void FixedUpdateState()
        {
            HandleSliding();
        }

        public override void LateUpdateState() { }

        #endregion

        #region State Logic

        private void HandleSliding()
        {
            Vector3 moveDir = Ctx.MoveDirection;

            Vector3 targetVelocity = moveDir * currentSlideSpeed;
            float accel = 20f;
            Ctx.Rigidbody.linearVelocity = Vector3.Lerp(Ctx.Rigidbody.linearVelocity, targetVelocity, Time.fixedDeltaTime * accel);

            if (moveDir.magnitude > 0.1f)
            {
                Vector3 rotationDir = new(moveDir.x, 0, moveDir.z);
                Quaternion targetRotation = Quaternion.LookRotation(rotationDir, Vector3.up);

                Ctx.SmoothModelRotation = Quaternion.Slerp(Ctx.PlayerModel.rotation, targetRotation, Time.fixedDeltaTime * 10f);
            }
        }

        #endregion

        public override void CheckSwitchState()
        {

        }
    }
}