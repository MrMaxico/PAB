using Entities.Base.States;

namespace Entities.Player.States
{
    /// <summary>
    /// Player-only state keys. Inherits the shared ones from <see cref="EntityStates"/>,
    /// so <c>PlayerStates.Grounded</c> and <c>PlayerStates.Walled</c> both resolve.
    /// </summary>
    public class PlayerStates : EntityStates
    {
        protected PlayerStates() { }

        // Root States
        public static readonly StateKey Walled = new("Walled");
        public static readonly StateKey Railed = new("Railed");
        public static readonly StateKey Barred = new("Barred");

        // Movement Sub-States
        public static readonly StateKey Diving = new("Diving");

        // Specialized Movement
        public static readonly StateKey Climbing = new("Climbing");
        public static readonly StateKey ClimbUp = new("ClimbUp");
        public static readonly StateKey WallWalking = new("WallWalking");
        public static readonly StateKey WallClinging = new("WallClinging");
        public static readonly StateKey WallLunging = new("WallLunging");
        public static readonly StateKey LedgeHanging = new("LedgeHanging");
        public static readonly StateKey Swinging = new("Swinging");

        // Camera Context States
        public static readonly StateKey ThirdPersonCamera = new("ThirdPersonCamera");
        public static readonly StateKey FirstPersonCamera = new("FirstPersonCamera");
    }
}
