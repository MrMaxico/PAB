using Entities.Base.States;
using Entities.Base.States.Base;

namespace Entities.Player.States.Base
{
    public abstract class PlayerContextState : EntityContextState
    {
        protected PlayerStateMachine Ctx { get; }

        protected PlayerContextState(PlayerStateMachine ctx, EntityStateFactory factory) : base(ctx, factory)
        {
            Ctx = ctx;
        }
    }
}
