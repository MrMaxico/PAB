using Entities.Player.Detection;
using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class WalledState : PlayerBaseState
    {
        private const string GroundCheck = "Ground";

        private const string RailCheck = "Rail";

        private const string FrontCheck = "Front";
        private const string RightCheck = "Right";
        private const string LeftCheck = "Left";
        private const string BackCheck = "Back";

        public WalledState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Walled;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
            Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");

            Ctx.GroundDetector.AddCheck(GroundCheck, Vector3.down, 0.6f, 0, CastType.SphereCast, radius: 0.5f);

            if (Factory.HasState(PlayerStates.Climbing) || Factory.HasState(PlayerStates.LedgeHanging))
            {
                Ctx.WallDetector.AddCheck(FrontCheck, Vector3.forward, 0.7f, 0, CastType.SphereCast, radius: 0.3f);
            }

            if (Factory.HasState(PlayerStates.WallWalking))
            {
                Ctx.WallDetector.AddCheck(RightCheck, Vector3.right, 0.8f, 1, CastType.Raycast);
                Ctx.WallDetector.AddCheck(LeftCheck, Vector3.left, 0.8f, 2, CastType.Raycast);
            }

            if (Factory.HasState(PlayerStates.WallClinging))
            {
                Ctx.WallDetector.AddCheck(BackCheck, Vector3.back, 0.8f, 3, CastType.Raycast);
            }

            Ctx.GroundDetector.Tick();
            Ctx.WallDetector.Tick();

            Ctx.Rigidbody.useGravity = false;
            Ctx.Rigidbody.linearVelocity = new Vector3(Ctx.Rigidbody.linearVelocity.x, 0f, Ctx.Rigidbody.linearVelocity.z);
        }

        public override void ExitState(PlayerBaseState nextState)
        {
            Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");

            Ctx.GroundDetector.RemoveCheck(GroundCheck);

            Ctx.WallDetector.RemoveCheck(FrontCheck);
            Ctx.WallDetector.RemoveCheck(RightCheck);
            Ctx.WallDetector.RemoveCheck(LeftCheck);
            Ctx.WallDetector.RemoveCheck(BackCheck);

            Ctx.Rigidbody.useGravity = true;
        }

        #region MonoBehaviours

        public override void UpdateState() { }

        public override void FixedUpdateState()
        {
            if (!Ctx.WallDetector.HasAnyHit()) return;

            Vector3 playerToWallPoint = Ctx.transform.position - Ctx.WallDetector.WallHit.point;
            float currentDist = Vector3.Dot(playerToWallPoint, Ctx.WallDetector.WallNormal);

            float targetDist = 0.4f;
            float distanceError = currentDist - targetDist;

            float forceStrength = distanceError > 0 ? 255f : 10f;

            Vector3 correctionForce = -Ctx.WallDetector.WallNormal * (distanceError * forceStrength);
            Ctx.Rigidbody.AddForce(correctionForce, ForceMode.Acceleration);
        }

        public override void LateUpdateState() { }

        #endregion

        #region Inputs

        #endregion

        private bool CheckAngle()
        {
            float angle = Vector3.Angle(Ctx.Orientation.forward, -Ctx.WallDetector.WallNormal);
            return angle > 35f;
        }

        public override void InitializeSubState()
        {

            if (Factory.HasState(PlayerStates.WallWalking))
            {
                if (Ctx.IsMovementInput && (Ctx.WallDetector.IsHit(RightCheck) || Ctx.WallDetector.IsHit(LeftCheck)))
                //if (Ctx.IsMovementInput && CheckAngle() && (Ctx.WallDetector.IsHit(RightCheck) || Ctx.WallDetector.IsHit(LeftCheck)))
                {
                    TrySwitchSubState(PlayerStates.WallWalking);
                    return;
                }
            }

            if (Factory.HasState(PlayerStates.WallClinging))
            {
                if (Ctx.IsRunInput)
                {
                    TrySwitchSubState(PlayerStates.WallClinging);
                    return;
                }
            }

            if (Factory.HasState(PlayerStates.Climbing))
            {
                TrySwitchSubState(PlayerStates.Climbing);
                return;
            }
        }

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Grounded))
            {
                if (Ctx.GroundDetector.HasAnyHit())
                {
                    TrySwitchState(PlayerStates.Grounded);
                    return;
                }
            }

            if (Factory.HasState(PlayerStates.Falling))
            {
                if (!Ctx.WallDetector.HasAnyHit())
                {
                    TrySwitchState(PlayerStates.Falling);
                    return;
                }
            }
        }
    }
}
