using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class WalkingState : MovementBaseState
    {
        // Step-up settings
        private const float MaxStepHeight = 0.4f;
        private const float StepCheckDepth = 0.5f;
        private const float StepForwardBoost = 2f;
        private const float CapsuleHalfHeight = 1f;
        private const float StepCastRadius = 0.3f;
        private const float StepUpGraceDuration = 0.15f;

        public WalkingState(PlayerStateMachine currentContext, PlayerStateFactory charachterStateFactory) : base(currentContext, charachterStateFactory)
        {
            StateKey = PlayerStates.Walking;
        }

        private Vector2 _moveInput;
        private float _moveDuration;

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            _moveDuration = 0f;
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
        }

        #region MonoBehaveiours

        public override void UpdateState()
        {
            _moveDuration += Time.deltaTime;
        }

        public override void FixedUpdateState()
        {
            HandleWalking();

            if (TryStepUp())
                return;

            if (Ctx.WallDetector.IsHit("Front"))
            {

                Vector3 playerMoveDir = Ctx.Rigidbody.linearVelocity.normalized;
                Vector3 wallNormal = Ctx.WallDetector.WallNormal;
                float dotProduct = Vector3.Dot(playerMoveDir, wallNormal);

                if (dotProduct < -0.5f)
                {
                    Debug.Log("Trying to switch to jump from walking into a wall");
                    TrySwitchRootState(PlayerStates.Jumping);
                }
            }
        }

        public override void LateUpdateState() { }

        #endregion

        #region Inputs

        #endregion

        #region State Logic

        private void HandleWalking()
        {
            Vector3 moveDir = Ctx.MoveDirection;

            Vector3 targetVelocity = moveDir * Ctx.PlayerContext.WalkSpeed;
            float accel = 20f;
            Ctx.Rigidbody.linearVelocity = Vector3.Lerp(Ctx.Rigidbody.linearVelocity, targetVelocity, Time.fixedDeltaTime * accel);

            if (moveDir.magnitude > 0.1f)
            {
                Vector3 rotationDir = new Vector3(moveDir.x, 0, moveDir.z);
                Quaternion targetRotation = Quaternion.LookRotation(rotationDir, Vector3.up);

                Ctx.PlayerObject.rotation = Quaternion.Slerp(Ctx.PlayerObject.rotation, targetRotation, Time.fixedDeltaTime * 10f);
            }
        }

        private bool TryStepUp()
        {
            Vector3 origin = Ctx.Transform.position;
            Vector3 footOrigin = origin + Vector3.down * CapsuleHalfHeight;
            Vector3 forward = Ctx.MoveDirection != Vector3.zero
                ? Ctx.MoveDirection.normalized
                : Ctx.Orientation.forward;

            // SphereCast at foot level — catches stairs approached at an angle
            Vector3 footCheckOrigin = footOrigin + Vector3.up * (StepCastRadius + 0.05f);
            if (!Physics.SphereCast(footCheckOrigin, StepCastRadius, forward, out RaycastHit footHit, StepCheckDepth))
            {
                return false;
            }

            Debug.DrawRay(footCheckOrigin, forward * StepCheckDepth, Color.yellow);

            // Raycast at step height — is the space above the step clear?
            Vector3 upperOrigin = footOrigin + Vector3.up * MaxStepHeight;
            Debug.DrawRay(upperOrigin, forward * StepCheckDepth, Color.red);
            if (Physics.Raycast(upperOrigin, forward, StepCheckDepth))
            {
                return false;
            }

            // Downcast to find the actual step surface
            Vector3 downCastOrigin = upperOrigin + forward * StepCheckDepth;
            Debug.DrawRay(downCastOrigin, Vector3.down * MaxStepHeight, Color.blue);
            if (!Physics.Raycast(downCastOrigin, Vector3.down, out RaycastHit stepHit, MaxStepHeight))
            {
                return false;
            }

            float heightDifference = stepHit.point.y - footOrigin.y;

            if (heightDifference < 0.01f || heightDifference > MaxStepHeight)
            {
                return false;
            }

            Vector3 targetPosition = new(origin.x, stepHit.point.y + CapsuleHalfHeight + 0.05f, origin.z);
            Ctx.Rigidbody.MovePosition(targetPosition);

            Ctx.Rigidbody.AddForce(forward * StepForwardBoost, ForceMode.VelocityChange);

            // Tell GroundedState not to snap us back down
            Ctx.StepUpGraceTime = StepUpGraceDuration;
            return true;
        }

        #endregion

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Running))
            {
                if (Ctx.IsMovementInput && Ctx.IsRunInput && Ctx.GroundDetector.HasAnyHit())
                {
                    TrySwitchState(PlayerStates.Running);
                    return;
                }
            }

            if (Factory.HasState(PlayerStates.Idling))
            {
                if (!Ctx.IsMovementInput)
                {
                    TrySwitchState(PlayerStates.Idling);
                    return;
                }
            }
        }
    }
}