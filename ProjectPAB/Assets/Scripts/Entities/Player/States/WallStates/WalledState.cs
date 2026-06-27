using Entities.Player.Detection;
using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class WalledState : PlayerBaseState
    {
        private const string GroundCheck = "Ground";

        private const string FrontCheck = "Front";
        private const string RightCheck = "Right";
        private const string LeftCheck = "Left";
        private const string BackCheck = "Back";

        public bool WallWalkSideRightLeft;

        public WalledState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Walled;
        }

        public override void EnterState(PlayerBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif

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
                Ctx.WallDetector.AddCheck(BackCheck, Vector3.back, 0.9f, 3, CastType.Raycast);
            }

            Ctx.GroundDetector.Tick();
            Ctx.WallDetector.Tick();

            Ctx.Rigidbody.useGravity = false;
            Ctx.Rigidbody.linearVelocity = new Vector3(Ctx.Rigidbody.linearVelocity.x, 0f, Ctx.Rigidbody.linearVelocity.z);
        }

        public override void ExitState(PlayerBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.GroundDetector.RemoveCheck(GroundCheck);

            Ctx.WallDetector.RemoveCheck(FrontCheck);
            Ctx.WallDetector.RemoveCheck(RightCheck);
            Ctx.WallDetector.RemoveCheck(LeftCheck);
            Ctx.WallDetector.RemoveCheck(BackCheck);

            Ctx.Rigidbody.useGravity = true;
        }

        #region MonoBehaviours

        public override void FixedUpdateState()
        {
            if (!Ctx.WallDetector.HasAnyHit()) return;

            Vector3 playerToWallPoint = Ctx.transform.position - Ctx.WallDetector.WallHit.point;
            float currentDist = Vector3.Dot(playerToWallPoint, Ctx.WallDetector.WallNormal);

            float targetDist = 0.4f;
            float distanceError = currentDist - targetDist;

            float forceStrength = distanceError > 0 ? 255f : 10f;

            // this does work but probably needs a better way to handle this
            if (MovementSubState.StateKey != PlayerStates.WallLunging)
            {
                Vector3 correctionForce = -Ctx.WallDetector.WallNormal * (distanceError * forceStrength);
                Ctx.Rigidbody.AddForce(correctionForce, ForceMode.Acceleration);
            }
        }

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
                    if (TrySwitchSubState(PlayerStates.WallWalking))
                    {
                        Ctx.WallDetector.RemoveCheck(RightCheck);
                        Ctx.WallDetector.RemoveCheck(LeftCheck);

                        Ctx.WallDetector.RemoveCheck(FrontCheck);
                        Ctx.WallDetector.RemoveCheck(BackCheck);

                        WallWalkSideRightLeft = Ctx.WallDetector.IsHit(LeftCheck);

                        return;
                    }
                }
            }

            if (Factory.HasState(PlayerStates.WallClinging))
            {
                if (Ctx.IsRunInput)
                {
                    if (TrySwitchSubState(PlayerStates.WallClinging))
                    {
                        Ctx.WallDetector.RemoveCheck(RightCheck);
                        Ctx.WallDetector.RemoveCheck(LeftCheck);

                        return;
                    }
                }
            }

            if (Factory.HasState(PlayerStates.Climbing))
            {
                if (TrySwitchSubState(PlayerStates.Climbing))
                {
                    Ctx.WallDetector.RemoveCheck(RightCheck);
                    Ctx.WallDetector.RemoveCheck(LeftCheck);

                    return;
                }
            }
        }

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Grounded))
            {
                if (Ctx.GroundDetector.HasAnyHit())
                {
                    if (TrySwitchState(PlayerStates.Grounded))
                        return;
                }
            }

            if (Factory.HasState(PlayerStates.Falling))
            {
                if (!Ctx.WallDetector.HasAnyHit())
                {
                    if (TrySwitchState(PlayerStates.Falling))
                        return;
                }
            }
        }
    }
}
