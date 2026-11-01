using UnityEngine;

namespace Entities.Base.Detection
{
    /// <summary>
    /// A suppression timer shared by several probes (<see cref="Probe.Gated"/>).
    /// Suppressing the gate makes every probe on it report no hit for that time,
    /// e.g. ignore all ground probes briefly after jumping off the ground.
    /// </summary>
    public sealed class ProbeGate
    {
        private double _suppressedUntil = double.NegativeInfinity;

        public void SuppressFor(float seconds) => _suppressedUntil = Time.timeAsDouble + seconds;

        public bool IsSuppressed => Time.timeAsDouble < _suppressedUntil;
    }
}
