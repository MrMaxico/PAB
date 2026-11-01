using UnityEngine;

namespace Entities.Base.Detection
{
    /// <summary>
    /// Probes checked in priority order (first = highest priority).
    /// Reading stops at the first hit, so lower-priority probes only cast when the ones before them miss.
    /// </summary>
    public sealed class ProbeGroup
    {
        /// <summary>A group that never hits. Use it instead of null when a state has no probes of a kind.</summary>
        public static readonly ProbeGroup Empty = new();

        private readonly Probe[] _probes;

        public ProbeGroup(params Probe[] probes) => _probes = probes;

        /// <summary>True if any probe hits.</summary>
        public bool IsHit
        {
            get
            {
                for (int i = 0; i < _probes.Length; i++)
                    if (_probes[i].IsHit) return true;
                return false;
            }
        }

        /// <summary>The first hit in priority order, or default when nothing hits.</summary>
        public RaycastHit Hit
        {
            get
            {
                TryGetHit(out RaycastHit hit);
                return hit;
            }
        }

        public bool TryGetHit(out RaycastHit hit)
        {
            for (int i = 0; i < _probes.Length; i++)
                if (_probes[i].TryGetHit(out hit)) return true;

            hit = default;
            return false;
        }
    }
}
