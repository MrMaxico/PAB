using Entities.Base.Detection;
using Entities.Base.States.Base;
using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class BarredState : PlayerBaseState
    {
        /// <summary>True when the player grabbed the bar from above (walks on it). False when from the side or below (swings).</summary>
        public bool EntryFromAbove { get; private set; }

        private readonly Probe _ground;

        public BarredState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Barred;

            _ground = currentContext.CreateGroundProbe(0.8f, 0.5f);
        }

        public override void EnterState(EntityBaseState previousState = null)
        {
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            if (!TryResolveBarAnchor(out Vector3 pivot, out Vector3 axis))
            {
                if (Ctx.DoDebug) Debug.LogWarning("BarredState: no bar found, bailing out.");
                if (Factory.HasState(PlayerStates.Airborne)) TrySwitchState(PlayerStates.Airborne);
                else if (Factory.HasState(PlayerStates.Grounded)) TrySwitchState(PlayerStates.Grounded);
                return;
            }

            Ctx.Ground = _ground;

            Ctx.BarPivot = pivot;
            Ctx.BarAxis = axis;

            Vector3 playerToBar = (pivot - Ctx.Rigidbody.position).normalized;
            float entryAngle = Vector3.Angle(Vector3.up, -playerToBar);
            EntryFromAbove = entryAngle <= Ctx.PlayerContext.BarTopEntryAngle;
        }

        public override void ExitState(EntityBaseState nextState = null)
        {
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");

            Ctx.CurrentBar           = null;
            Ctx.CurrentBarAttachment = null;
            Ctx.BarPivot             = Vector3.zero;
            Ctx.BarAxis              = Vector3.right;
        }

        public override void InitializeSubState()
        {
            if (EntryFromAbove)
            {
                if (Factory.HasState(PlayerStates.Balancing))
                {
                    if (TrySwitchSubState(PlayerStates.Balancing))
                        return;
                }
            }
            else
            {
                if (Factory.HasState(PlayerStates.Swinging))
                {
                    if (TrySwitchSubState(PlayerStates.Swinging))
                        return;
                }
            }

            if (Factory.HasState(PlayerStates.Balancing))
            {
                if (TrySwitchSubState(PlayerStates.Balancing))
                    return;
            }

            if (Factory.HasState(PlayerStates.Swinging))
            {
                if (TrySwitchSubState(PlayerStates.Swinging))
                    return;
            }
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

            if (bestCollider == null)
            {
                pivot = default;
                axis = default;
                return false;
            }

            Ctx.CurrentBar = bestCollider.gameObject;

            pivot = closest;
            axis = bestCollider.transform.right;
            if (axis.sqrMagnitude < 0.0001f) axis = Vector3.right;
            axis.Normalize();

            // If the bar has a BarAttachment, use it for precise centerline pivot and axis.
            BarAttachment attachment = bestCollider != null
                ? bestCollider.GetComponent<BarAttachment>()
                : null;

            Ctx.CurrentBarAttachment = attachment;

            if (attachment != null)
            {
                pivot = attachment.GetClosestPointOnAxis(reference);
                axis = attachment.Axis;
            }

            return true;
        }
    }
}