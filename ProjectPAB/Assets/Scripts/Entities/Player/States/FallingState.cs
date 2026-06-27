using Entities.Player.Detection;
using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;

namespace Entities.Player.States
{
    public class FallingState : PlayerBaseState
    {
        private const string GroundCheck = "Ground";

        private const string RailCheck = "Rail";

        private const string FrontCheck = "Front";
        private const string RightCheck = "Right";
        private const string LeftCheck = "Left";

        public FallingState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Falling;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.GroundDetector.AddCheck(GroundCheck, Vector3.down, 0.8f, 0, CastType.SphereCast, radius: 0.5f);

            Ctx.RailDetector.AddCheck(RailCheck, Vector3.down, 0.8f, 0, CastType.SphereCast, radius: 0.35f);

            Ctx.WallDetector.AddCheck(FrontCheck, Vector3.forward, 0.7f, 0, CastType.SphereCast, radius: 0.3f);

            if (Factory.HasState(PlayerStates.WallWalking))
            {
                Ctx.WallDetector.AddCheck(RightCheck, Vector3.right, 0.7f, 0, CastType.Raycast);
                Ctx.WallDetector.AddCheck(LeftCheck, Vector3.left, 0.7f, 0, CastType.Raycast);
            }

            Ctx.GroundDetector.Tick();
            Ctx.RailDetector.Tick();
            Ctx.WallDetector.Tick();
        }

        public override void ExitState(PlayerBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.GroundDetector.RemoveCheck(GroundCheck);

            Ctx.RailDetector.RemoveCheck(RailCheck);

            Ctx.WallDetector.RemoveCheck(FrontCheck);
            Ctx.WallDetector.RemoveCheck(RightCheck);
            Ctx.WallDetector.RemoveCheck(LeftCheck);
        }

        #region MonoBehaviours

        #endregion

        #region Inputs

        protected override void HandleInputAction(IInputProvider input)
        {
            if (Factory.HasState(PlayerStates.Jumping))
            {
                if (Ctx.JumpsLeft > 0 && Ctx.GroundDetector.CoyoteTimeCounter > 0)
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
                if (Ctx.RailDetector.HasAnyHit())
                {
                    if (TrySwitchState(PlayerStates.Railed))
                        return;
                }
            }

            if (Factory.HasState(PlayerStates.Grounded))
            {
                if (Ctx.GroundDetector.HasAnyHit())
                {
                    if (TrySwitchState(PlayerStates.Grounded))
                        return;
                }
            }

            if (Factory.HasState(PlayerStates.Walled))
            {
                if (Ctx.WallDetector.HasAnyHit())
                {
                    if (TrySwitchState(PlayerStates.Walled))
                        return;
                }
            }
        }
    }
}
