namespace Entities.Base.States
{
    /// <summary>
    /// State keys any entity can use. Entity-specific key sets inherit from this
    /// (e.g. <c>PlayerStates : EntityStates</c>) so <c>PlayerStates.Grounded</c> resolves too.
    /// </summary>
    public class EntityStates
    {
        protected EntityStates() { }

        // Root States
        public static readonly StateKey Grounded = new("Grounded");
        public static readonly StateKey Airborne = new("Airborne");
        public static readonly StateKey Jumping = new("Jumping");

        // Movement Sub-States
        public static readonly StateKey Idling = new("Idling");
        public static readonly StateKey Walking = new("Walking");
        public static readonly StateKey Running = new("Running");
        public static readonly StateKey Falling = new("Falling");

        // Combat/Action Keys (Generic slots for swapped weapons)
        public static readonly StateKey PrimaryAction = new("PrimaryAction");
        public static readonly StateKey SecondaryAction = new("SecondaryAction");
        public static readonly StateKey UtilityAction = new("UtilityAction");
    }
}
