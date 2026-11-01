using Entities.Base.Detection;
using Entities.Base.States.Base;
using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class JumpingState : PlayerBaseState
    {
        private readonly Probe _ground;
        private readonly Probe _rail;
        private readonly ProbeGroup _walls;
        private readonly Probe _bar;

        public JumpingState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Jumping;

            _ground = currentContext.CreateGroundProbe(0.55f, 0.3f).Named("Jumping_Ground");
            _rail = currentContext.CreateRailProbe(0.8f, 0.35f).Named("Jumping_Rail");
            _walls = new ProbeGroup(
                currentContext.CreateWallSphere(Vector3.forward, 0.7f, 0.3f),
                currentContext.CreateWallRay(Vector3.right, 0.7f).When(() => Factory.HasState(PlayerStates.WallWalking)),
                currentContext.CreateWallRay(Vector3.left, 0.7f).When(() => Factory.HasState(PlayerStates.WallWalking)));
            _bar = currentContext.CreateBarProbe(Vector3.up, 0f, 3f).Named("Jumping_Bar");
        }

        public override void EnterState(EntityBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState.StateKey}");
#endif

            Ctx.Ground = _ground;
            Ctx.Walls = _walls;
            Ctx.Bar = _bar;

            Ctx.JumpsUsed++;

            Ctx.JumpToFallingTime = Ctx.MaxJumpToFallingTime;
            Ctx.WalkJumpToWalledTime = Ctx.MaxWalkJumpToWalledTime;
            Ctx.JumpToWalledTime = Ctx.MaxJumpToWalledTime;

            Ctx.LastGroundedTime = float.NegativeInfinity;

            if (previousState.StateKey == PlayerStates.Grounded)
            {
                Ctx.AirStrafeSpeedBoost = true;
            }

            Vector3 vel = Ctx.Rigidbody.linearVelocity;
            vel.y = 0;
            Ctx.Rigidbody.linearVelocity = vel;
            Ctx.Rigidbody.AddForce(Ctx.JumpDirection * 7f, ForceMode.Impulse);
        }

        public override void ExitState(EntityBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState.StateKey}");
#endif
        }

        #region MonoBehaviours

        public override void UpdateState()
        {
            Ctx.JumpToFallingTime -= Time.deltaTime;
            Ctx.WalkJumpToWalledTime -= Time.deltaTime;
            Ctx.JumpToWalledTime -= Time.deltaTime;
        }

        #endregion

        #region Inputs

        protected override void HandleInputAction(IInputProvider input)
        {
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
                if (Ctx.IsMovementInput && _ground.IsHit)
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
            if (Factory.HasState(PlayerStates.Airborne))
            {
                if (Ctx.JumpToFallingTime <= 0)
                {
                    TrySwitchState(PlayerStates.Airborne);
                    return;
                }
            }

            if (Factory.HasState(PlayerStates.Walled))
            {
                if (_walls.IsHit && Ctx.JumpToWalledTime <= 0)
                {
                    TrySwitchState(PlayerStates.Walled);
                    return;
                }
            }

            if (Factory.HasState(PlayerStates.Railed))
            {
                if (_rail.IsHit)
                {
                    TrySwitchState(PlayerStates.Railed);
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
                    TrySwitchState(PlayerStates.Grounded);
                    return;
                }
            }
        }
    }
}
