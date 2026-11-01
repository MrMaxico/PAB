using Entities.Base.States;
using Entities.Base.States.Base;

namespace Entities.Player.States.Base
{
    public abstract class PlayerActionState : EntityActionState
    {
        protected PlayerStateMachine Ctx { get; }

        protected PlayerActionState(PlayerStateMachine ctx, EntityStateFactory factory) : base(ctx, factory)
        {
            Ctx = ctx;
        }
    }
}
