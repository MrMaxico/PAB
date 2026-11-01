using Entities.Base.Detection;
using Entities.Base.States.Base;
using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class AirborneState : PlayerBaseState
    {
        private readonly Probe _ground;
        private readonly Probe _rail;
        private readonly ProbeGroup _walls;
        private readonly Probe _bar;

        public AirborneState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Airborne;

            _ground = currentContext.CreateGroundProbe(0.8f, 0.5f).Named("Airborne_Ground");
            _rail = currentContext.CreateRailProbe(0.8f, 0.35f).Named("Airborne_Rail");
            _walls = new ProbeGroup(
                currentContext.CreateWallSphere(Vector3.forward, 0.7f, 0.3f),
                currentContext.CreateWallRay(Vector3.right, 0.7f).When(() => Factory.HasState(PlayerStates.WallWalking)),
                currentContext.CreateWallRay(Vector3.left, 0.7f).When(() => Factory.HasState(PlayerStates.WallWalking)));
            _bar = currentContext.CreateBarProbe(Vector3.up, 0f, 3f).Named("Airborne_Bar");
        }

        public override void EnterState(EntityBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.Ground = _ground;
            Ctx.Walls = _walls;
            Ctx.Bar = _bar;
        }

        public override void ExitState(EntityBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif
        }

        #region Inputs

        protected override void HandleInputAction(IInputProvider input)
        {
            if (Factory.HasState(PlayerStates.Jumping))
            {
                if (Ctx.JumpsLeft > 0 && Ctx.InCoyoteTime)
                {
                    if (input.JumpState.UseBufferedPress())
                    {
                        if (TrySwitchState(PlayerStates.Jumping))
                            return;
                    }
                }
            }

            if (Factory.HasState(PlayerStates.Diving))
            {
                if (input.DiveState.IsPressed)
                {
                    if (TrySwitchState(PlayerStates.Diving))
                        return;
                }
            }
        }

        #endregion

        public override void InitializeSubState()
        {
            if (Factory.HasState(PlayerStates.Falling))
            {
                if (Ctx.IsMovementInput)
                {
                    if (TrySwitchSubState(PlayerStates.Falling))
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

            if (Factory.HasState(PlayerStates.Barred))
            {
                if (_bar.IsHit)
                {
                    if (TrySwitchState(PlayerStates.Barred))
                        return;
                }
            }

            if (Factory.HasState(PlayerStates.Grounded))
            {
                if (_ground.IsHit)
                {
                    if (TrySwitchState(PlayerStates.Grounded))
                        return;
                }
            }

            if (Factory.HasState(PlayerStates.Walled))
            {
                if (_walls.IsHit)
                {
                    if (TrySwitchState(PlayerStates.Walled))
                        return;
                }
            }
        }
    }
}
