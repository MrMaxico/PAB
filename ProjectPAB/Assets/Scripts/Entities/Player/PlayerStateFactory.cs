using Entities.Player.States;
using Entities.Player.States.Base;
using System.Collections.Generic;

namespace Entities.Player
{
    public enum PlayerStates
    {
        None,

        // Root States
        Grounded,
        Falling,
        Jumping,
        Walled,

        // Movement Sub-States
        Idling,
        Walking,
        Running,
        Sliding,

        // Specialized Movement
        Climbing,
        ClimbUp,
        WallWalking,
        WallClinging,
        WallLunging,

        // Combat/Action Keys (Generic slots for swapped weapons)
        PrimaryAction,
        SecondaryAction,
        UtilityAction,

        // Camera Context States
        ThirdPersonCamera,
        FirstPersonCamera
    }

    public class PlayerStateFactory
    {
        private readonly PlayerStateMachine _context;
        private readonly Dictionary<PlayerStates, PlayerBaseState> _states;

        // HashSet to "hide" states without deleting the object (prevents GC spikes)
        private readonly HashSet<PlayerStates> _disabledStates;

        public PlayerStateFactory(PlayerStateMachine currentContext)
        {
            _context = currentContext;
            _states = new Dictionary<PlayerStates, PlayerBaseState>();
            _disabledStates = new HashSet<PlayerStates>();

            // --- Root States ---
            RegisterState(PlayerStates.Grounded, new GroundedState(_context, this));
            RegisterState(PlayerStates.Falling, new FallingState(_context, this));
            RegisterState(PlayerStates.Jumping, new JumpingState(_context, this));
            RegisterState(PlayerStates.Walled, new WalledState(_context, this));

            // --- Locomotion States ---
            RegisterState(PlayerStates.Idling, new IdlingState(_context, this));
            RegisterState(PlayerStates.Walking, new WalkingState(_context, this));
            RegisterState(PlayerStates.Running, new RunningState(_context, this));
            RegisterState(PlayerStates.Sliding, new SlidingState(_context, this));

            // --- Climbing/Wall States ---
            RegisterState(PlayerStates.Climbing, new ClimbingState(_context, this));
            RegisterState(PlayerStates.WallWalking, new WallWalkingState(_context, this));
            RegisterState(PlayerStates.WallClinging, new WallClingingState(_context, this));
            RegisterState(PlayerStates.WallLunging, new WallLungingState(_context, this));
            RegisterState(PlayerStates.ClimbUp, new ClimbUpState(_context, this));

            // --- Camera Context States ---
            RegisterState(PlayerStates.ThirdPersonCamera, new ThirdPersonCameraState(_context, this));
            RegisterState(PlayerStates.FirstPersonCamera, new FirstPersonCameraState(_context, this));

            // Note: PrimaryAction is usually registered dynamically when a weapon is equipped.
        }

        /// <summary>
        /// Registers a new state instance. Returns false if the key is already taken.
        /// </summary>
        public bool RegisterState(PlayerStates stateKey, PlayerBaseState stateInstance)
        {
            if (!_states.ContainsKey(stateKey))
            {
                _states.Add(stateKey, stateInstance);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a state from the dictionary entirely.
        /// </summary>
        public bool UnregisterState(PlayerStates stateKey)
        {
            return _states.Remove(stateKey);
        }

        /// <summary>
        /// Overwrites an existing state key with a new instance. 
        /// Use this for swapping weapons (e.g., swapping a SwordState for a MaceState).
        /// </summary>
        public void SwapState(PlayerStates stateKey, PlayerBaseState newInstance)
        {
            _states[stateKey] = newInstance;
        }

        /// <summary>
        /// Hides or unhides a state. If hidden, GetState returns null, 
        /// triggering your BaseState auto-eviction logic.
        /// </summary>
        public void SetStateAvailability(PlayerStates stateKey, bool isAvailable)
        {
            if (isAvailable) _disabledStates.Remove(stateKey);
            else _disabledStates.Add(stateKey);
        }

        /// <summary>
        /// Retrieves the state instance. Returns null if hidden or non-existent.
        /// </summary>
        public PlayerBaseState GetState(PlayerStates state)
        {
            if (_disabledStates.Contains(state)) return null;

            if (_states.TryGetValue(state, out PlayerBaseState stateInstance))
            {
                return stateInstance;
            }
            return null;
        }

        public bool HasState(PlayerStates state)
        {
            return _states.ContainsKey(state) && !_disabledStates.Contains(state);
        }
    }
}