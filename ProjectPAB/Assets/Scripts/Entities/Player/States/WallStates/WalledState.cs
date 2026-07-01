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

            Ctx.GroundDetector.AddSphere(GroundCheck, 0.6f, 0.5f);

            if (Factory.HasState(PlayerStates.Climbing) || Factory.HasState(PlayerStates.LedgeHanging))
            {
                Ctx.WallDetector.AddSphere(FrontCheck, Vector3.forward, 0.7f, 0.3f);
            }

            if (Factory.HasState(PlayerStates.WallWalking))
            {
                Ctx.WallDetector.AddRay(RightCheck, Vector3.right, 0.8f, 1);
                Ctx.WallDetector.AddRay(LeftCheck, Vector3.left, 0.8f, 2);
            }

            if (Factory.HasState(PlayerStates.WallClinging))
            {
                Ctx.WallDetector.AddRay(BackCheck, Vector3.back, 0.9f, 3);
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
            UpdatePlatformVelocity();

            if (!Ctx.WallDetector.HasAnyHit()) return;

            Vector3 playerToWallPoint = Ctx.transform.position - Ctx.WallDetector.Hit.Point;
            float currentDist = Vector3.Dot(playerToWallPoint, Ctx.WallDetector.Hit.Normal);

            float targetDist = 0.4f;
            float distanceError = currentDist - targetDist;

            float forceStrength = distanceError > 0 ? 255f : 10f;

            if (MovementSubState.StateKey != PlayerStates.WallLunging)
            {
                Vector3 correctionForce = -Ctx.WallDetector.Hit.Normal * (distanceError * forceStrength);
                Ctx.Rigidbody.AddForce(correctionForce, ForceMode.Acceleration);
            }
        }

        #endregion

        #region Inputs

        #endregion

        private Transform _trackedPlatformTransform;

        private void UpdatePlatformVelocity()
        {
            DetectionHit hit = Ctx.WallDetector.Hit;
            IMovingPlatform platform = hit.Platform;
            bool isTrackingPlatform = platform != null && hit.Transform == _trackedPlatformTransform;

            Ctx.PlatformVelocity = isTrackingPlatform
                ? platform.DeltaThisStep / Time.fixedDeltaTime
                : Vector3.zero;

            _trackedPlatformTransform = hit.Transform;
        }

        private bool CheckAngle()
        {
            float angle = Vector3.Angle(Ctx.Orientation.forward, -Ctx.WallDetector.Hit.Normal);
            return angle > 35f;
        }

        public override void InitializeSubState()
        {
            if (Factory.HasState(PlayerStates.WallWalking))
            {
                if (Ctx.IsMovementInput && (Ctx.WallDetector.IsHit(RightCheck) || Ctx.WallDetector.IsHit(LeftCheck)))
                {
                    if (TrySwitchSubState(PlayerStates.WallWalking))
                    {
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
