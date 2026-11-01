using Entities.Base.States.Base;
using System.Collections.Generic;
using UnityEngine;

namespace Entities.Base.States
{
    /// <summary>
    /// Runtime registry of state instances for one entity.
    /// Entity-specific factories inherit from this and register their states in their constructor.
    /// </summary>
    public class EntityStateFactory
    {
        private readonly Dictionary<StateKey, EntityBaseState> _states = new();

        // Lets network code turn a StateKey.Id back into the key instance.
        private readonly Dictionary<int, StateKey> _keysById = new();

        // HashSet to "hide" states without deleting the object (prevents GC spikes)
        private readonly HashSet<StateKey> _disabledStates = new();

        /// <summary>
        /// Registers a new state instance. Returns false if the key is already taken.
        /// </summary>
        public bool RegisterState(StateKey stateKey, EntityBaseState stateInstance)
        {
            if (!_states.TryAdd(stateKey, stateInstance)) return false;

            TrackKey(stateKey);
            return true;
        }

        /// <summary>
        /// Removes a state from the dictionary entirely.
        /// </summary>
        public bool UnregisterState(StateKey stateKey)
        {
            return _states.Remove(stateKey);
        }

        /// <summary>
        /// Overwrites an existing state key with a new instance.
        /// Use this for swapping weapons (e.g., swapping a SwordState for a MaceState).
        /// An active instance that gets swapped out is evicted on the next update.
        /// </summary>
        public void SwapState(StateKey stateKey, EntityBaseState newInstance)
        {
            _states[stateKey] = newInstance;
            TrackKey(stateKey);
        }

        /// <summary>
        /// Hides or unhides a state. If hidden, GetState returns null,
        /// triggering the base state's auto-eviction logic.
        /// </summary>
        public void SetStateAvailability(StateKey stateKey, bool isAvailable)
        {
            if (isAvailable) _disabledStates.Remove(stateKey);
            else _disabledStates.Add(stateKey);
        }

        /// <summary>
        /// Retrieves the state instance. Returns null if hidden or non-existent.
        /// </summary>
        public EntityBaseState GetState(StateKey stateKey)
        {
            if (stateKey == null || _disabledStates.Contains(stateKey)) return null;

            return _states.TryGetValue(stateKey, out EntityBaseState stateInstance) ? stateInstance : null;
        }

        public bool HasState(StateKey stateKey)
        {
            return stateKey != null && _states.ContainsKey(stateKey) && !_disabledStates.Contains(stateKey);
        }

        /// <summary>
        /// Resolves a key from its <see cref="StateKey.Id"/> (e.g. one received over the network).
        /// Returns null if no state was ever registered under that key.
        /// </summary>
        public StateKey GetKey(int id)
        {
            return _keysById.TryGetValue(id, out StateKey key) ? key : null;
        }

        private void TrackKey(StateKey stateKey)
        {
            if (_keysById.TryGetValue(stateKey.Id, out StateKey existing) && existing != stateKey)
            {
                Debug.LogError($"State key id collision between '{existing}' and '{stateKey}'. Rename one of them.");
                return;
            }

            _keysById[stateKey.Id] = stateKey;
        }
    }
}
