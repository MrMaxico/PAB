using Entities.Base.States;

namespace Entities.Player.States
{
    public class PlayerStateFactory : EntityStateFactory
    {
        public PlayerStateFactory(PlayerStateMachine context)
        {
            // --- Root States ---
            RegisterState(PlayerStates.Grounded, new GroundedState(context, this));
            RegisterState(PlayerStates.Airborne, new AirborneState(context, this));
            RegisterState(PlayerStates.Jumping, new JumpingState(context, this));
            RegisterState(PlayerStates.Walled, new WalledState(context, this));
            RegisterState(PlayerStates.Railed, new RailedState(context, this));
            RegisterState(PlayerStates.Barred, new BarredState(context, this));

            // --- Locomotion States ---
            RegisterState(PlayerStates.Idling, new IdlingState(context, this));
            RegisterState(PlayerStates.Walking, new WalkingState(context, this));
            RegisterState(PlayerStates.Running, new RunningState(context, this));

            RegisterState(PlayerStates.Diving, new DivingState(context, this));

            RegisterState(PlayerStates.Falling, new FallingState(context, this));

            RegisterState(PlayerStates.Swinging, new SwingingState(context, this));


            // --- Climbing/Wall States ---
            RegisterState(PlayerStates.Climbing, new ClimbingState(context, this));
            RegisterState(PlayerStates.WallWalking, new WallWalkingState(context, this));
            RegisterState(PlayerStates.WallClinging, new WallClingingState(context, this));
            RegisterState(PlayerStates.WallLunging, new WallLungingState(context, this));
            RegisterState(PlayerStates.LedgeHanging, new LedgeHangState(context, this));
            RegisterState(PlayerStates.ClimbUp, new ClimbUpState(context, this));

            // --- Camera Context States ---
            RegisterState(PlayerStates.ThirdPersonCamera, new ThirdPersonCameraState(context, this));
            RegisterState(PlayerStates.FirstPersonCamera, new FirstPersonCameraState(context, this));

            // Note: PrimaryAction is usually registered dynamically when a weapon is equipped.
        }
    }
}
