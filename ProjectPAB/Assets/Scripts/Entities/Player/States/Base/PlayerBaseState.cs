using Entities.Base.States;
using Entities.Base.States.Base;

namespace Entities.Player.States.Base
{
    /// <summary>
    /// Root state for the player. All hierarchy logic lives in <see cref="EntityBaseState"/>;
    /// this only adds the typed player context.
    /// </summary>
    public abstract class PlayerBaseState : EntityBaseState
    {
        protected PlayerStateMachine Ctx { get; }

        protected PlayerBaseState(PlayerStateMachine ctx, EntityStateFactory factory) : base(ctx, factory)
        {
            Ctx = ctx;
        }
    }
}
