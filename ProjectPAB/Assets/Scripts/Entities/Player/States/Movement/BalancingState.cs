using Entities.Base.States.Base;
using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class BalancingState : PlayerMovementState
    {
        private const float StandingOffset = 1f;

        public BalancingState(PlayerStateMachine currentContext, PlayerStateFactory charachterStateFactory) : base(currentContext, charachterStateFactory)
        {
            StateKey = PlayerStates.Balancing;
        }

        public override void EnterState(EntityBaseState previousState)
        {
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            if (Ctx.CurrentBar != null)
            {
                Vector3 entryPos = GetBarTopPosition(Ctx.BarPivot);
                Ctx.Rigidbody.position = entryPos;
            }

            Ctx.Rigidbody.linearVelocity = Vector3.zero;
            Ctx.Rigidbody.useGravity = false;
        }

        public override void ExitState(EntityBaseState nextState)
        {
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");

            Ctx.Rigidbody.useGravity = true;
        }

        #region Inputs

        private Vector2 _moveInput;

        protected override void HandleInputAction(IInputProvider input)
        {
            _moveInput = input.MovementState.RawInputValue;
            Vector3 rawInput = (Ctx.Orientation.forward * _moveInput.y) + (Ctx.Orientation.right * _moveInput.x);

            Ctx.MoveDirection = Vector3.ProjectOnPlane(rawInput, Vector3.up).normalized;

            if (Factory.HasState(PlayerStates.Jumping))
            {
                if (input.JumpState.UseBufferedPress())
                {
                    if (TrySwitchState(PlayerStates.Jumping))
                    {
                        Ctx.BarGate.SuppressFor(Ctx.PlayerContext.BarJumpGrace);
                        return;
                    }
                }
            }
        }

        #endregion

        public override void FixedUpdateState()
        {
            HandleBalancing();
        }

        public override void CheckSwitchState()
        {
            // Only consider transitioning when the player is actively moving toward an edge.
            bool leavingEnd  = Mathf.Abs(_barMoveIntent) > 0.1f && !_barAhead;
            bool leavingSide = _sideMoveIntent > 0.1f;
            if (!leavingEnd && !leavingSide) return;

            // Suppress the bar gate so the player doesn't instantly re-attach after stepping off.

            // Bar ended — transition to grounded if the ground is there, otherwise fall.
            if (Factory.HasState(PlayerStates.Grounded) && Ctx.Ground != null && Ctx.Ground.IsHit)
            {
                if (TrySwitchRootState(PlayerStates.Grounded))
                {
                    Ctx.BarGate.SuppressFor(Ctx.PlayerContext.BarGroundedGrace);
                    return;
                }
            }

            if (Factory.HasState(PlayerStates.Airborne))
            {
                if (TrySwitchRootState(PlayerStates.Airborne))
                {
                    Ctx.BarGate.SuppressFor(Ctx.PlayerContext.BarAirborneGrace);
                    return;
                }
            }
        }

        private const float FrontCheckDistance = 0.4f;
        private const float DownCheckDistance = 1.5f;
        private const float CheckRadius = 0.2f;

        // Shared between HandleBalancing and CheckSwitchState (same physics tick).
        private float _barMoveIntent;
        private float _sideMoveIntent;
        private bool _barAhead;

        private void HandleBalancing()
        {
            // Keep axis in sync with a rotating/moving bar.
            if (Ctx.CurrentBarAttachment != null)
                Ctx.BarAxis = Ctx.CurrentBarAttachment.Axis;

            // Project input onto the bar axis for movement, but rotate freely with full input direction.
            _barMoveIntent = Vector3.Dot(Ctx.MoveDirection, Ctx.BarAxis);
            Vector3 barMoveDir = Ctx.BarAxis * _barMoveIntent;

            // Perpendicular-to-bar component — lets the player step off the short side.
            Vector3 sideDir = Vector3.ProjectOnPlane(Ctx.MoveDirection, Ctx.BarAxis);
            _sideMoveIntent = sideDir.magnitude;

            // Free rotation: face wherever the player pushes, not just along the bar axis.
            if (Ctx.MoveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(Ctx.MoveDirection, Vector3.up);
                Ctx.SmoothModelRotation = Quaternion.Slerp(Ctx.PlayerModel.rotation, targetRotation, Time.fixedDeltaTime * 10f);
            }

            // Check if bar continues in the movement direction.
            bool isMoving = Mathf.Abs(_barMoveIntent) > 0.1f;
            _barAhead = false;
            if (isMoving)
            {
                Vector3 castOrigin = Ctx.Rigidbody.position + (barMoveDir.normalized * FrontCheckDistance);
                _barAhead = Physics.SphereCast(
                    castOrigin,
                    CheckRadius,
                    Vector3.down,
                    out RaycastHit _,
                    DownCheckDistance,
                    Ctx.PlayerContext.BarLayer,
                    QueryTriggerInteraction.Collide);

                Debug.DrawLine(castOrigin, castOrigin + Vector3.down * DownCheckDistance, _barAhead ? Color.green : Color.red);
            }

            // Move along the bar axis and, when stepping sideways, add lateral velocity.
            Vector3 velocity = Ctx.BarAxis * (_barMoveIntent * Ctx.PlayerContext.WalkSpeed);
            if (_sideMoveIntent > 0.1f)
                velocity += sideDir.normalized * (_sideMoveIntent * Ctx.PlayerContext.WalkSpeed);
            Ctx.Rigidbody.linearVelocity = velocity;

            // Lock position to the bar top only while still on it.
            // Release when moving off the end OR stepping sideways off the short side.
            bool leavingSide = _sideMoveIntent > 0.1f;
            if (Ctx.CurrentBar != null && (!isMoving || _barAhead) && !leavingSide)
            {
                Ctx.Rigidbody.position = GetBarTopPosition(Ctx.Rigidbody.position);
            }
        }

        /// <summary>
        /// Projects <paramref name="worldPos"/> onto the bar's axis line and returns the
        /// point directly on top of the bar at that position.
        /// </summary>
        private Vector3 GetBarTopPosition(Vector3 worldPos)
        {
            Vector3 barCenter = Ctx.CurrentBar.transform.position;
            float t = Vector3.Dot(worldPos - barCenter, Ctx.BarAxis);
            float standHeight = Ctx.CurrentBarAttachment != null
                ? Ctx.CurrentBarAttachment.Radius
                : StandingOffset;
            return barCenter + Ctx.BarAxis * t + Vector3.up * standHeight;
        }
    }
}
