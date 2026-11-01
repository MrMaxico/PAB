using Entities.Base.Detection;
using Entities.Base.States.Base;
using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class BarredState : PlayerBaseState
    {
        public Vector3 BottomTangent { get; private set; } = Vector3.forward;
        public float FacingSign { get; private set; } = 1f;
        public float EntryAngle { get; private set; }
        public float EntryAngularVelocity { get; private set; }

        public BarredState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Barred;
        }

        public override void EnterState(EntityBaseState previousState = null)
        {
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            if (!TryResolveBarAnchor(out Vector3 pivot, out Vector3 axis))
                return;

            Ctx.BarPivot = pivot;
            Ctx.BarAxis = axis;

            float length = Mathf.Max(0.5f, Ctx.PlayerContext.SwingLength);

            Vector3 radial = Ctx.Rigidbody.position - pivot;
            if (radial.sqrMagnitude < 0.0001f) radial = Vector3.down;
            radial.Normalize();

            // Capture pre-grab velocity for swing entry momentum.
            Vector3 entryVelocity = Ctx.Rigidbody.linearVelocity;

            Vector3 tangent = Vector3.Cross(axis, radial).normalized;
            BottomTangent = Vector3.Cross(axis, Vector3.down).normalized;
            if (BottomTangent.sqrMagnitude < 0.0001f)
                BottomTangent = Vector3.forward;

            EntryAngle = Vector3.SignedAngle(Vector3.down, radial, axis) * Mathf.Deg2Rad;

            float entryTangentialSpeed = Vector3.Dot(entryVelocity, tangent);
            EntryAngularVelocity = entryTangentialSpeed / length;

            Vector3 entryFwd = Vector3.ProjectOnPlane(Ctx.PlayerModel.forward, Vector3.up);
            if (entryFwd.sqrMagnitude < 0.0001f)
                entryFwd = Vector3.ProjectOnPlane(entryVelocity, Vector3.up);

            if (entryFwd.sqrMagnitude < 0.0001f)
                entryFwd = BottomTangent;

            FacingSign = Vector3.Dot(entryFwd, BottomTangent) >= 0f ? 1f : -1f;

            Ctx.Rigidbody.useGravity = false;
            Ctx.Rigidbody.linearVelocity = Vector3.zero;
            Ctx.Rigidbody.position = pivot + (radial * length);
        }

        public override void ExitState(EntityBaseState nextState = null)
        {
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
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

        }

        private bool TryResolveBarAnchor(out Vector3 pivot, out Vector3 axis)
        {
            Probe barProbe = Ctx.Bar;
            Vector3 center = barProbe != null ? barProbe.LastOrigin : Ctx.Rigidbody.position;
            float radius = barProbe != null ? barProbe.Radius : 3f;

            Collider[] hits = Physics.OverlapSphere(
                center,
                radius,
                Ctx.PlayerContext.BarLayer,
                QueryTriggerInteraction.Collide);

            if (hits.Length == 0)
            {
                pivot = default;
                axis = default;
                return false;
            }

            Vector3 reference = Ctx.Rigidbody.position;
            float best = float.PositiveInfinity;
            Vector3 closest = reference;
            Collider bestCollider = null;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i];
                if (col == null) continue;

                Vector3 p = col.ClosestPoint(reference);
                float d = (p - reference).sqrMagnitude;

                if (d < best)
                {
                    best = d;
                    closest = p;
                    bestCollider = col;
                }
            }

            pivot = closest;
            axis = bestCollider != null ? bestCollider.transform.right : Ctx.PlayerModel.right;
            if (axis.sqrMagnitude < 0.0001f) axis = Vector3.right;
            axis.Normalize();

            return true;
        }
    }
}