using Entities.Player.Detection;
using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class BarredState : PlayerBaseState
    {
        private float GrabRadius => Ctx.PlayerContext.BarArmLength + 1f;

        public bool HasBar { get; private set; }
        public Vector3 BarPivot { get; private set; }
        public Vector3 BarAxis { get; private set; }

        // Captured once on grab, not per frame.
        public BarApproach Approach { get; private set; }
        public BarOrientation Orientation { get; private set; }

        // Swing plane, perpendicular to the bar. Built in GrabBar.
        public Vector3 PlaneDown;     // "straight down" inside the plane -> theta = 0
        public Vector3 PlaneTangent;  // horizontal swing direction inside the plane -> +theta

        // Captured before going kinematic, which wipes rigidbody velocity.
        public Vector3 EntryVelocity { get; private set; }

        public BarredState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Barred;
        }

        public override void EnterState(PlayerBaseState previousState = null)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif

            EntryVelocity = Ctx.Rigidbody.linearVelocity;

            Ctx.Rigidbody.useGravity = false;
            Ctx.Rigidbody.isKinematic = true;

            GrabBar();
        }

        public override void ExitState(PlayerBaseState nextState = null)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.BarDetector.RegisterReleaseTime();

            Ctx.Rigidbody.useGravity = true;
            Ctx.Rigidbody.isKinematic = false;
            HasBar = false;
        }

        public override void FixedUpdateState()
        {
            if (Ctx.BarDetector.TryFindBar(Ctx.transform.position, GrabRadius, out var hit))
            {
                BarPivot = hit.GrabPoint;
                HasBar = true;
            }
            else
            {
                HasBar = false;
            }
        }

        private void GrabBar()
        {
            var hit = Ctx.BarDetector.Hit;
            if (!hit.IsHit && !Ctx.BarDetector.TryFindBar(Ctx.transform.position, GrabRadius, out hit))
            {
                HasBar = false;
                return;
            }

            BarPivot = hit.GrabPoint;
            BarAxis = hit.Axis;
            Approach = hit.Approach;
            Orientation = hit.Orientation;

            PlaneDown = Vector3.ProjectOnPlane(Physics.gravity, BarAxis).normalized;
            if (PlaneDown.sqrMagnitude < 0.0001f) PlaneDown = Vector3.down;
            PlaneTangent = Vector3.Cross(BarAxis, PlaneDown).normalized;

            Vector3 facing = Vector3.ProjectOnPlane(Ctx.PlayerModel.forward, BarAxis);
            if (Vector3.Dot(PlaneTangent, facing) < 0f) PlaneTangent = -PlaneTangent;

            HasBar = true;
        }

        public override void InitializeSubState()
        {
            if (Factory.HasState(PlayerStates.Swinging))
            {
                if (TrySwitchSubState(PlayerStates.Swinging))
                    return;
            }
        }

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Falling))
            {
                if (!HasBar)
                {
                    if (TrySwitchState(PlayerStates.Falling))
                        return;
                }
            }
        }
    }
}
