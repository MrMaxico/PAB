using System;
using System.Collections.Generic;
using UnityEngine;

namespace Entities.Base.Detection
{
    public enum ProbeShape
    {
        Ray,
        Sphere,
        OverlapSphere,
    }

    /// <summary>
    /// A single lazily-evaluated physics query. It only casts when <see cref="IsHit"/> or
    /// <see cref="Hit"/> is read, and at most once per physics step; later reads in the same
    /// step return the cached result.
    /// <para>
    /// Create probes once (in a state's constructor) with the fluent API and keep them in a field.
    /// There is nothing to register or remove: a probe nobody reads never casts.
    /// </para>
    /// </summary>
    public sealed class Probe
    {
        #region Shape

        public string DebugName { get; private set; } = "Probe";
        public Rigidbody Body { get; }
        public ProbeShape Shape { get; }
        public float Distance { get; }
        public float Radius { get; }
        public LayerMask Mask { get; private set; } = ~0;
        public QueryTriggerInteraction Triggers { get; private set; } = QueryTriggerInteraction.Ignore;

        private readonly Vector3 _direction;
        private Transform _space;
        private Func<Vector3> _dynamicDirection;
        private Vector3 _originOffset;
        private Func<bool> _condition;
        private ProbeGate _gate;

        #endregion

        #region Result

        private double _stamp = -1;
        private double _suppressedUntil = double.NegativeInfinity;
        private bool _isHit;
        private RaycastHit _hit;

        /// <summary>World origin used by the most recent cast (for debugging).</summary>
        public Vector3 LastOrigin { get; private set; }

        /// <summary>World direction used by the most recent cast (zero if it was skipped).</summary>
        public Vector3 LastDirection { get; private set; }

        #endregion

        private Probe(Rigidbody body, ProbeShape shape, Vector3 direction, float distance, float radius)
        {
            Body = body;
            Shape = shape;
            _direction = direction;
            Distance = distance;
            Radius = radius;
        }

        #region Creation

        /// <summary>A raycast from the body's physics position.</summary>
        public static Probe Ray(Rigidbody body, Vector3 direction, float distance) =>
            new(body, ProbeShape.Ray, direction, distance, 0f);

        /// <summary>A sphere cast from the body's physics position.</summary>
        public static Probe Sphere(Rigidbody body, Vector3 direction, float distance, float radius) =>
            new(body, ProbeShape.Sphere, direction, distance, radius);

        /// <summary>A sphere overlap check. The sphere center is moved by direction * centerOffset.</summary>
        public static Probe OverlapSphere(Rigidbody body, Vector3 direction, float centerOffset, float radius) =>
            new(body, ProbeShape.OverlapSphere, direction, centerOffset, radius);

        /// <summary>Only hit these layers. Defaults to everything.</summary>
        public Probe On(LayerMask mask) { Mask = mask; return this; }

        /// <summary>Treat the direction as local to this transform (e.g. the player model).</summary>
        public Probe RelativeTo(Transform space) { _space = space; return this; }

        /// <summary>
        /// Read the direction at cast time, e.g. <c>() => ctx.MoveDirection</c>.
        /// Overrides the fixed direction. A zero direction skips the cast (no hit).
        /// The delegate is created once here, so this does not allocate per cast.
        /// </summary>
        public Probe Along(Func<Vector3> direction) { _dynamicDirection = direction; return this; }

        /// <summary>World-space offset from the body position, e.g. <c>Vector3.up * 0.1f</c>.</summary>
        public Probe Offset(Vector3 worldOffset) { _originOffset = worldOffset; return this; }

        /// <summary>Also hit trigger colliders.</summary>
        public Probe IncludeTriggers() { Triggers = QueryTriggerInteraction.Collide; return this; }

        /// <summary>
        /// Only cast while this returns true, e.g. <c>() => Factory.HasState(PlayerStates.WallWalking)</c>.
        /// Useful inside a <see cref="ProbeGroup"/>, where the read itself isn't behind an availability check.
        /// </summary>
        public Probe When(Func<bool> condition) { _condition = condition; return this; }

        /// <summary>Share a suppression timer with other probes (e.g. all ground probes ignore the ground after a jump).</summary>
        public Probe Gated(ProbeGate gate) { _gate = gate; return this; }

        /// <summary>For debugging: give a name to this probe instance.</summary>
        public Probe Named(string debugName)
        {
            DebugName = string.IsNullOrWhiteSpace(debugName) ? "Probe" : debugName;
            return this;
        }

        #endregion

        #region Control

        /// <summary>Report no hit for the given time (replaces the detectors' jump grace).</summary>
        public void SuppressFor(float seconds)
        {
            _suppressedUntil = Time.timeAsDouble + seconds;
            Invalidate();
        }

        public bool IsSuppressed => Time.timeAsDouble < _suppressedUntil || (_gate != null && _gate.IsSuppressed);

        /// <summary>Force a fresh cast on the next read (e.g. right after teleporting the body).</summary>
        public void Invalidate() => _stamp = -1;

        #endregion

        #region Results

        public bool IsHit
        {
            get { Evaluate(); return _isHit; }
        }

        /// <summary>The last hit. Only meaningful when <see cref="IsHit"/> is true.</summary>
        public RaycastHit Hit
        {
            get { Evaluate(); return _hit; }
        }

        public bool TryGetHit(out RaycastHit hit)
        {
            Evaluate();
            hit = _hit;
            return _isHit;
        }

        #endregion

        #region Evaluation

        private void Evaluate()
        {
            double now = Time.fixedTimeAsDouble;
            if (_stamp == now) return;
            _stamp = now;

            if (_condition != null && !_condition())
            {
                SetNoHit();
                return;
            }

#if UNITY_EDITOR
            RecordForDebug(now);
#endif

            if (IsSuppressed)
            {
                SetNoHit();
                return;
            }

            Vector3 baseOrigin = Body.position + _originOffset;

            Vector3 dir = _dynamicDirection != null ? _dynamicDirection()
                        : _space != null ? _space.TransformDirection(_direction)
                        : _direction;

            // OverlapSphere: fixed area check (not a sweep)
            if (Shape == ProbeShape.OverlapSphere)
            {
                Vector3 normalizedDir = dir.sqrMagnitude < 0.0001f ? Vector3.zero : dir.normalized;
                Vector3 center = baseOrigin + (normalizedDir * Distance);

                LastOrigin = center;
                LastDirection = Vector3.zero;

                _isHit = Physics.CheckSphere(center, Radius, Mask, Triggers);
                _hit = default;
                return;
            }

            LastOrigin = baseOrigin;

            if (dir.sqrMagnitude < 0.0001f)
            {
                SetNoHit();
                return;
            }

            dir.Normalize();
            LastDirection = dir;

            _isHit = Shape == ProbeShape.Sphere
                ? Physics.SphereCast(LastOrigin, Radius, dir, out _hit, Distance, Mask, Triggers)
                : Physics.Raycast(LastOrigin, dir, out _hit, Distance, Mask, Triggers);
        }

        private void SetNoHit()
        {
            LastDirection = Vector3.zero;
            _isHit = false;
            _hit = default;
        }

        #endregion

        #region Debug
#if UNITY_EDITOR

        // Probes that cast during the latest physics step, drawn by ProbeDebugDrawer.
        internal static readonly List<Probe> EvaluatedThisStep = new();
        private static double _debugStamp = -1;

        private void RecordForDebug(double now)
        {
            if (_debugStamp != now)
            {
                EvaluatedThisStep.Clear();
                _debugStamp = now;
            }

            EvaluatedThisStep.Add(this);
        }

        // Cached result without triggering a cast, so gizmo drawing never causes physics queries.
        internal bool DebugIsHit => _isHit;
        internal RaycastHit DebugHit => _hit;

#endif
        #endregion
    }
}
