using Entities.Base.Detection;
using Entities.Base.States.Base;
using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class GroundedState : PlayerBaseState
    {
        private readonly Probe _ground;
        private readonly Probe _rail;
        private readonly ProbeGroup _walls;

        private const float CapsuleHalfHeight = 1f;
        private const float SnapDownDistance = 0.5f;
        private const float UngroundedTolerance = 0.15f;

        private float _ungroundedTimer;

        public GroundedState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Grounded;

            _ground = currentContext.CreateGroundProbe(0.8f, 0.5f);
            _rail = currentContext.CreateRailProbe(1f, 0.35f);
            _walls = new ProbeGroup(
                currentContext.CreateWallSphere(Vector3.forward, 0.7f, 0.3f)
                    .When(() => Factory.HasState(PlayerStates.Jumping))); // only feeds Walking's auto-jump into walls
        }

        public override void EnterState(EntityBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.Ground = _ground;
            Ctx.Walls = _walls;

            Ctx.Rigidbody.useGravity = false;

            if (!_ground.Hit.IsSloped())
            {
                Vector3 vel = Ctx.Rigidbody.linearVelocity;
                Ctx.Rigidbody.linearVelocity = new Vector3(vel.x, 0, vel.z);
            }

            Ctx.JumpsUsed = 0;
            Ctx.JumpDirection = Vector3.up;
            _ungroundedTimer = 0f;

            SnapToGround(true);
        }

        public override void ExitState(EntityBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.Rigidbody.useGravity = true;

            Ctx.PlatformVelocity = Vector3.zero;
            _trackedPlatformTransform = null;
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
            if (_ground.IsHit)
                Ctx.LastGroundedTime = Time.time;

            UpdatePlatformVelocity();

            Vector3 rawInput = (Ctx.Orientation.forward * _currentInput.y) + (Ctx.Orientation.right * _currentInput.x);

            Vector3 movementNormal = Vector3.up;

            if (_ground.Hit.normal != Vector3.zero)
            {
                float surfaceAngle = Vector3.Angle(Vector3.up, _ground.Hit.normal);

                if (surfaceAngle <= Ctx.PlayerContext.MaxSlopeAngle)
                {
                    movementNormal = _ground.Hit.normal;
                }
            }

            Ctx.MoveDirection = Vector3.ProjectOnPlane(rawInput, movementNormal).normalized;

            if (_ground.Hit.IsSloped() && Vector3.Angle(Vector3.up, _ground.Hit.normal) <= 45f)
            {
                Vector3 groundNormal = _ground.Hit.normal;
                Ctx.Rigidbody.AddForce(-groundNormal * 30f, ForceMode.Force);
            }

            SnapToGround();
            CheckStepUp();

            // find better way to fix last step up velocity leak. WORKS FOR NOW
            if (!_ground.Hit.IsSloped())
            {
                Vector3 velocity = Ctx.Rigidbody.linearVelocity;
                velocity.y = _surfaceVerticalVelocity;
                Ctx.Rigidbody.linearVelocity = velocity;
            }
        }

        #endregion

        #region State Logic

        private Transform _trackedPlatformTransform;
        private float _surfaceVerticalVelocity;

        private void UpdatePlatformVelocity()
        {
            RaycastHit hit = _ground.Hit;
            IMovingPlatform platform = hit.Platform();
            bool isTrackingPlatform = platform != null && hit.transform == _trackedPlatformTransform;

            Vector3 platformVelocity = isTrackingPlatform ? platform.DeltaThisStep / Time.fixedDeltaTime : Vector3.zero;

            Ctx.PlatformVelocity = new Vector3(platformVelocity.x, 0f, platformVelocity.z);

            _surfaceVerticalVelocity = isTrackingPlatform ? platformVelocity.y : 0f;

            if (isTrackingPlatform)
            {
                Vector3 vel = Ctx.Rigidbody.linearVelocity;
                Ctx.Rigidbody.linearVelocity = new Vector3(vel.x, platformVelocity.y, vel.z);
            }

            _trackedPlatformTransform = hit.transform;
        }

        private void SnapToGround(bool overrideRequirements = false)
        {
            if (!overrideRequirements)
            {
                if (_ground.IsHit)
                    return;

                if (Ctx.StepUpGraceTime > 0f)
                    return;
            }

            Vector3 origin = Ctx.Rigidbody.position;
            Vector3 footOrigin = origin + Vector3.down * CapsuleHalfHeight;

            if (Physics.Raycast(footOrigin + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, SnapDownDistance + 0.1f, Ctx.PlayerContext.GroundLayer))
            {
                float gap = footOrigin.y - hit.point.y;

                if (gap > 0.01f && gap <= SnapDownDistance)
                {
                    Vector3 snappedPos = new(origin.x, hit.point.y + CapsuleHalfHeight, origin.z);
                    Ctx.Rigidbody.MovePosition(snappedPos);

                    // Same MovePosition velocity leak as step-up; reset to the surface's vertical velocity.
                    Vector3 velocity = Ctx.Rigidbody.linearVelocity;
                    velocity.y = _surfaceVerticalVelocity;
                    Ctx.Rigidbody.linearVelocity = velocity;
                }
            }
        }

        private void CheckStepUp()
        {
            if (Ctx.MoveDirection.magnitude < 0.1f) return;

            float capsuleRadius = Ctx.PlayerContext.PlayerRadius;
            float maxStepHeight = Ctx.PlayerContext.MaxStepHeight;
            float inset = Ctx.PlayerContext.StepInset;
            float checkDistance = Ctx.PlayerContext.StepCheckDistance + inset;
            float treadProbeForward = Ctx.PlayerContext.TreadProbeForward;

            Vector3 moveDir = new Vector3(Ctx.MoveDirection.x, 0f, Ctx.MoveDirection.z).normalized;
            Vector3 footOrigin = Ctx.Rigidbody.position + Vector3.down * CapsuleHalfHeight;

            Vector3 sideDir = Vector3.Cross(Vector3.up, moveDir).normalized;
            float spreadWidth = capsuleRadius * 0.6f;

            Vector3[] horizontalOffsets = new Vector3[]
            {
                Vector3.zero,
                sideDir * spreadWidth,
                -sideDir * spreadWidth
            };

            foreach (Vector3 horizontalOffset in horizontalOffsets)
            {
                Vector3 rayOriginBase = footOrigin + (moveDir * (capsuleRadius - inset)) + horizontalOffset;

                Vector3 lowerOrigin = rayOriginBase + (Vector3.up * 0.05f);
                Vector3 upperOrigin = rayOriginBase + (Vector3.up * maxStepHeight);

                if (!Physics.Raycast(lowerOrigin, moveDir, out RaycastHit lowerHit, checkDistance, Ctx.PlayerContext.GroundLayer))
                    continue;

                if (Vector3.Angle(Vector3.up, lowerHit.normal) <= Ctx.PlayerContext.MaxSlopeAngle)
                    continue;

                float clearanceDistance = lowerHit.distance + treadProbeForward;
                if (Physics.Raycast(upperOrigin, moveDir, clearanceDistance, Ctx.PlayerContext.GroundLayer))
                    continue;

                Vector3 treadLookOrigin = new Vector3(lowerHit.point.x, upperOrigin.y, lowerHit.point.z)
                                          + (moveDir * treadProbeForward);

                if (!Physics.Raycast(treadLookOrigin, Vector3.down, out RaycastHit treadHit, maxStepHeight + 0.05f, Ctx.PlayerContext.GroundLayer))
                    continue;

                float exactStepHeight = treadHit.point.y - footOrigin.y;

                if (exactStepHeight > 0.02f && exactStepHeight <= maxStepHeight)
                {
                    float climbSpeed = Ctx.IsRunInput ? Ctx.PlayerContext.RunSpeed : Ctx.PlayerContext.WalkSpeed;
                    float stepDelta = climbSpeed * Time.fixedDeltaTime / 1.15f;
                    float rise = Mathf.Min(exactStepHeight + 0.03f, stepDelta);

                    bool clearsStep = rise >= exactStepHeight;

                    Vector3 targetPosition;
                    if (clearsStep)
                    {
                        targetPosition = Ctx.Rigidbody.position + (moveDir * stepDelta);
                        targetPosition.y = treadHit.point.y + CapsuleHalfHeight;
                    }
                    else
                    {
                        targetPosition = Ctx.Rigidbody.position + (Vector3.up * rise);
                    }

                    Ctx.Rigidbody.MovePosition(targetPosition);

                    Vector3 velocity = Ctx.Rigidbody.linearVelocity;
                    velocity.y = _surfaceVerticalVelocity;
                    Ctx.Rigidbody.linearVelocity = velocity;

                    Ctx.StepUpGraceTime = 0.15f;
                    return;
                }
            }
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
                        {
                            Ctx.GroundGate.SuppressFor(Ctx.PlayerContext.GroundJumpGrace);
                            return;
                        }
                    }
                }
            }
        }

        #endregion

        public override void InitializeSubState()
        {
            if (Factory.HasState(PlayerStates.Walking))
            {
                if (Ctx.IsMovementInput && _ground.IsHit)
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
            if (Factory.HasState(PlayerStates.Railed))
            {
                if (_rail.IsHit)
                {
                    if (TrySwitchState(PlayerStates.Railed))
                        return;
                }
            }

            if (Factory.HasState(PlayerStates.Airborne))
            {
                if (!_ground.IsHit)
                {
                    if (Ctx.StepUpGraceTime > 0f)
                    {
                        _ungroundedTimer = 0f;
                        return;
                    }

                    _ungroundedTimer += Time.fixedDeltaTime;
                    if (_ungroundedTimer >= UngroundedTolerance)
                    {
                        if (TrySwitchState(PlayerStates.Airborne))
                            return;
                    }
                }
                else
                {
                    _ungroundedTimer = 0f;
                }
            }

        }
    }
}
