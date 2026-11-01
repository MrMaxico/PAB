using Entities.Base.States;
using Entities.Base.States.Base;

namespace Entities.Player.States.Base
{
    public abstract class PlayerMovementState : EntityMovementState
    {
        protected PlayerStateMachine Ctx { get; }

        protected PlayerMovementState(PlayerStateMachine ctx, EntityStateFactory factory) : base(ctx, factory)
        {
            Ctx = ctx;
        }
    }
}
