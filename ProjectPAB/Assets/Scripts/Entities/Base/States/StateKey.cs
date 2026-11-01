namespace Entities.Base.States
{
    /// <summary>
    /// Identifies a state slot in an <see cref="EntityStateFactory"/>.
    /// Keys are compared by reference, so always use the static instances
    /// (<see cref="EntityStates"/>, <c>PlayerStates</c>, ...) instead of creating new ones.
    /// </summary>
    public sealed class StateKey
    {
        public string Name { get; }

        /// <summary>
        /// Stable id derived from <see cref="Name"/>. Identical on every machine and build,
        /// so it is safe to send over the network. Resolve it back with <see cref="EntityStateFactory.GetKey"/>.
        /// </summary>
        public int Id { get; }

        public StateKey(string name)
        {
            Name = name;
            Id = StableHash(name);
        }

        public override string ToString() => Name;

        // FNV-1a. string.GetHashCode() is not guaranteed to match between processes.
        private static int StableHash(string s)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in s)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return (int)hash;
            }
        }
    }
}
