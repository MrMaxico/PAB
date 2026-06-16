using Entities.Player.Detection;
using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class JumpingState : PlayerBaseState
    {
        private const string GroundCheck = "Ground";

        private const string RailCheck = "Rail";

        private const string FrontCheck = "Front";
        private const string RightCheck = "Right";
        private const string LeftCheck = "Left";

        public JumpingState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Jumping;
            StateType = PlayerStateType.Root;
        }

        Rigidbody _rb;

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState.StateKey}");

            Ctx.GroundDetector.AddCheck(GroundCheck, Vector3.down, 0.55f, 0, CastType.SphereCast, radius: 0.3f);

            Ctx.RailDetector.AddCheck(RailCheck, Vector3.down, 0.8f, 0, CastType.SphereCast, radius: 0.35f);

            Ctx.WallDetector.AddCheck(FrontCheck, Vector3.forward, 0.7f, 0, CastType.Raycast, radius: 0.3f);


            if (Factory.HasState(PlayerStates.WallWalking))
            {
                Ctx.WallDetector.AddCheck(RightCheck, Vector3.right, 0.7f, 0, CastType.Raycast);
                Ctx.WallDetector.AddCheck(LeftCheck, Vector3.left, 0.7f, 0, CastType.Raycast);
            }

            Ctx.GroundDetector.Tick();
            Ctx.WallDetector.Tick();

            Ctx.JumpsUsed++;

            Ctx.JumpToFallingTime = Ctx.MaxJumpToFallingTime;
            Ctx.WalkJumpToWalledTime = Ctx.MaxWalkJumpToWalledTime;
            Ctx.JumpToWalledTime = Ctx.MaxJumpToWalledTime;

            //Ctx.ConsumeJumpBuffer();
            Ctx.GroundDetector.ResetCoyoteTime();

            if (previousState.StateKey == PlayerStates.Grounded)
            {
                Ctx.GroundDetector.RegisterJumpTime();
            }
            else if (previousState.StateKey == PlayerStates.Walled)
            {
                Ctx.WallDetector.RegisterJumpTime();
            }
            else if (previousState.StateKey == PlayerStates.Railed)
            {
                Ctx.RailDetector.RegisterJumpTime();
            }

            _rb = Ctx.Rigidbody;

            Vector3 vel = _rb.linearVelocity;
            vel.y = 0;
            _rb.linearVelocity = vel;
            _rb.AddForce(Ctx.JumpDirection * 7f, ForceMode.Impulse);
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState.StateKey}");

            Ctx.GroundDetector.RemoveCheck(GroundCheck);

            Ctx.WallDetector.RemoveCheck(FrontCheck);
            Ctx.WallDetector.RemoveCheck(RightCheck);
            Ctx.WallDetector.RemoveCheck(LeftCheck);
        }

        #region MonoBehaviours

        public override void UpdateState()
        {
            Ctx.JumpToFallingTime -= Time.deltaTime;
            Ctx.WalkJumpToWalledTime -= Time.deltaTime;
            Ctx.JumpToWalledTime -= Time.deltaTime;
        }

        public override void FixedUpdateState() { }

        #endregion

        #region Inputs

        #endregion

        public override void InitializeSubState()
        {
            if (Factory.HasState(PlayerStates.Idling))
            {
                TrySwitchSubState(PlayerStates.Idling);
                return;
            }
        }

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Falling))
            {
                if (Ctx.JumpToFallingTime <= 0)
                {
                    TrySwitchState(PlayerStates.Falling);
                    return;
                }
            }

            if (Factory.HasState(PlayerStates.Walled))
            {
                if (Ctx.WallDetector.HasAnyHit() && Ctx.JumpToWalledTime <= 0)
                {
                    TrySwitchState(PlayerStates.Walled);
                    return;
                }
            }

            if (Factory.HasState(PlayerStates.Railed))
            {
                if (Ctx.RailDetector.HasAnyHit())
                {
                    TrySwitchState(PlayerStates.Railed);
                    return;
                }
            }

            if (Factory.HasState(PlayerStates.Grounded))
            {
                if (Ctx.GroundDetector.HasAnyHit())
                {
                    TrySwitchState(PlayerStates.Grounded);
                    return;
                }
            }
        }
    }
}
