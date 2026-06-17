using Entities.Player.Detection;
using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class GroundedState : PlayerBaseState
    {
        private const string GroundCheck = "Ground";
        private const string RailCheck = "Rail";
        private const string FrontCheck = "Front";

        private const float CapsuleHalfHeight = 1f;
        private const float SnapDownDistance = 0.5f;
        private const float UngroundedTolerance = 0.15f;

        private float _ungroundedTimer;

        public GroundedState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Grounded;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            Ctx.GroundDetector.AddCheck(GroundCheck, Vector3.down, 0.8f, 0, CastType.SphereCast, radius: 0.5f);

            Ctx.WallDetector.AddCheck(FrontCheck, Vector3.forward, 0.7f, 0, CastType.Raycast, radius: 0.3f);

            Ctx.RailDetector.AddCheck(RailCheck, Vector3.down, 1f, 0, CastType.SphereCast, radius: 0.35f);

            Ctx.GroundDetector.Tick();
            Ctx.WallDetector.Tick();

            Ctx.Rigidbody.useGravity = false;

            if (!Ctx.GroundDetector.IsSloped)
            {
                Vector3 vel = Ctx.Rigidbody.linearVelocity;
                Ctx.Rigidbody.linearVelocity = new Vector3(vel.x, 0, vel.z);
            }

            Ctx.JumpsUsed = 0;
            Ctx.JumpDirection = Vector3.up;
            _ungroundedTimer = 0f;

            SnapToGround(true);
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");

            Ctx.GroundDetector.RemoveCheck(GroundCheck);

            Ctx.WallDetector.RemoveCheck(FrontCheck);

            Ctx.RailDetector.RemoveCheck(RailCheck);

            Ctx.Rigidbody.useGravity = true;
        }

        #region MonoBehaviours

        public override void UpdateState()
        {
            if (Ctx.Stamina < Ctx.MaxStamina)
                Ctx.Stamina += Time.deltaTime * 15f;
            else
                Ctx.Stamina = Ctx.MaxStamina;
        }

        public override void FixedUpdateState()
        {
            Vector3 rawInput = (Ctx.Orientation.forward * _currentInput.y) + (Ctx.Orientation.right * _currentInput.x);

            Vector3 groundNormal = Ctx.GroundDetector.Normal == Vector3.zero ? Vector3.up : Ctx.GroundDetector.Normal;

            Ctx.MoveDirection = Vector3.ProjectOnPlane(rawInput, groundNormal).normalized;

            if (Ctx.GroundDetector.IsSloped)
            {
                Ctx.Rigidbody.AddForce(-groundNormal * 30f, ForceMode.Force);
            }

            SnapToGround();
        }

        #endregion

        #region Inputs

        private Vector2 _currentInput;

        protected override void HandleInputAction(IInputProvider input)
        {
            _currentInput = input.MovementState.RawInputValue;

            if (Factory.HasState(PlayerStates.Jumping))
            {
                if (Ctx.JumpsLeft > 0)
                {
                    if (input.JumpState.UseBufferedPressOrHold())
                    {
                        if (TrySwitchState(PlayerStates.Jumping))
                            return;
                    }
                }
            }
        }

        #endregion

        public override void InitializeSubState()
        {
            if (Factory.HasState(PlayerStates.Walking))
            {
                if (Ctx.IsMovementInput && Ctx.GroundDetector.HasAnyHit())
                {
                    if (TrySwitchSubState(PlayerStates.Walking))
                        return;
                }
            }

            if (Factory.HasState(PlayerStates.Idling))
            {
                if (TrySwitchSubState(PlayerStates.Idling))
                    return;
            }
        }

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Falling))
            {
                if (!Ctx.GroundDetector.HasAnyHit() && !Ctx.RailDetector.HasAnyHit())
                {
                    // Don't count frames during step-up grace period
                    if (Ctx.StepUpGraceTime > 0f)
                    {
                        _ungroundedTimer = 0f;
                        return;
                    }

                    _ungroundedTimer += Time.fixedDeltaTime;
                    if (_ungroundedTimer >= UngroundedTolerance)
                    {
                        if (TrySwitchState(PlayerStates.Falling))
                            return;
                    }
                }
                else
                {
                    _ungroundedTimer = 0f;
                }
            }

            if (Factory.HasState(PlayerStates.Railed))
            {
                if (Ctx.RailDetector.HasAnyHit())
                {
                    if (TrySwitchState(PlayerStates.Railed))
                        return;
                }
            }
        }

        #region Private Helpers

        private void SnapToGround(bool overrideRequirements = false)
        {
            if (!overrideRequirements)
            {
                if (Ctx.GroundDetector.HasAnyHit())
                    return;

                if (Ctx.StepUpGraceTime > 0f)
                    return;
            }

            Vector3 origin = Ctx.Transform.position;
            Vector3 footOrigin = origin + Vector3.down * CapsuleHalfHeight;

            if (Physics.Raycast(footOrigin + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, SnapDownDistance + 0.1f, Ctx.GroundDetector.GroundLayer))
            {
                float gap = footOrigin.y - hit.point.y;

                if (gap > 0.01f && gap <= SnapDownDistance)
                {
                    Vector3 snappedPos = new(origin.x, hit.point.y + CapsuleHalfHeight, origin.z);
                    Ctx.Rigidbody.MovePosition(snappedPos);
                }
            }
        }

        #endregion
    }
}
