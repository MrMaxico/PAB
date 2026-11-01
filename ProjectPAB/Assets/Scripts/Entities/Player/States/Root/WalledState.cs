using Entities.Base.Detection;
using Entities.Base.States.Base;
using Entities.Player.States.Base;
using UnityEngine;

namespace Entities.Player.States
{
    public class WalledState : PlayerBaseState
    {
        private readonly Probe _ground;
        private readonly Probe _right;
        private readonly Probe _left;
        private readonly ProbeGroup _walls;

        public bool WallWalkSideRightLeft;

        public WalledState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory) : base(currentContext, stateFactory)
        {
            StateKey = PlayerStates.Walled;

            _ground = currentContext.CreateGroundProbe(0.6f, 0.5f);
            _right = currentContext.CreateWallRay(Vector3.right, 0.8f).When(() => Factory.HasState(PlayerStates.WallWalking));
            _left = currentContext.CreateWallRay(Vector3.left, 0.8f).When(() => Factory.HasState(PlayerStates.WallWalking));
            _walls = new ProbeGroup(
                currentContext.CreateWallSphere(Vector3.forward, 0.7f, 0.3f)
                    .When(() => Factory.HasState(PlayerStates.Climbing) || Factory.HasState(PlayerStates.LedgeHanging)),
                _right,
                _left,
                currentContext.CreateWallRay(Vector3.back, 0.9f).When(() => Factory.HasState(PlayerStates.WallClinging)));
        }

        public override void EnterState(EntityBaseState previousState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Entered {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. From {previousState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.Ground = _ground;
            Ctx.Walls = _walls;

            Ctx.Rigidbody.useGravity = false;
            Ctx.Rigidbody.linearVelocity = new Vector3(Ctx.Rigidbody.linearVelocity.x, 0f, Ctx.Rigidbody.linearVelocity.z);
        }

        public override void ExitState(EntityBaseState nextState)
        {
#if UNITY_EDITOR
            if (Ctx.DoDebug) Debug.Log($"Exited {StateKey} with super state: {CurrentSuperState?.StateKey.ToString() ?? "null"}. To {nextState?.StateKey.ToString() ?? "null"}");
#endif

            Ctx.Rigidbody.useGravity = true;
        }

        #region MonoBehaviours

        public override void FixedUpdateState()
        {
            UpdatePlatformVelocity();

            if (!_walls.IsHit) return;

            Vector3 playerToWallPoint = Ctx.Rigidbody.position - _walls.Hit.point;
            float currentDist = Vector3.Dot(playerToWallPoint, _walls.Hit.normal);

            float targetDist = 0.4f;
            float distanceError = currentDist - targetDist;

            float forceStrength = distanceError > 0 ? 255f : 10f;

            if (MovementSubState?.StateKey != PlayerStates.WallLunging)
            {
                Vector3 correctionForce = -_walls.Hit.normal * (distanceError * forceStrength);
                Ctx.Rigidbody.AddForce(correctionForce, ForceMode.Acceleration);
            }
        }

        #endregion

        #region State Logic

        private Transform _trackedPlatformTransform;

        private void UpdatePlatformVelocity()
        {
            RaycastHit hit = _walls.Hit;
            IMovingPlatform platform = hit.Platform();
            bool isTrackingPlatform = platform != null && hit.transform == _trackedPlatformTransform;

            Ctx.PlatformVelocity = isTrackingPlatform
                ? platform.DeltaThisStep / Time.fixedDeltaTime
                : Vector3.zero;

            _trackedPlatformTransform = hit.transform;
        }

        private bool CheckAngle()
        {
            float angle = Vector3.Angle(Ctx.Orientation.forward, -_walls.Hit.normal);
            return angle > 35f;
        }

        #endregion

        public override void InitializeSubState()
        {
            if (Factory.HasState(PlayerStates.WallWalking))
            {
                if (Ctx.IsMovementInput && (_right.IsHit || _left.IsHit))
                {
                    if (TrySwitchSubState(PlayerStates.WallWalking))
                    {
                        WallWalkSideRightLeft = _left.IsHit;

                        return;
                    }
                }
            }

            if (Factory.HasState(PlayerStates.WallClinging))
            {
                if (Ctx.IsRunInput)
                {
                    if (TrySwitchSubState(PlayerStates.WallClinging))
                        return;
                }
            }

            if (Factory.HasState(PlayerStates.Climbing))
            {
                if (TrySwitchSubState(PlayerStates.Climbing))
                    return;
            }
        }

        public override void CheckSwitchState()
        {
            if (Factory.HasState(PlayerStates.Grounded))
            {
                if (_ground.IsHit)
                {
                    if (TrySwitchState(PlayerStates.Grounded))
                        return;
                }
            }

            if (Factory.HasState(PlayerStates.Airborne))
            {
                if (!_walls.IsHit)
                {
                    if (TrySwitchState(PlayerStates.Airborne))
                        return;
                }
            }
        }
    }
}
