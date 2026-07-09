using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Entities.Player.Detection
{
    public abstract class BaseDetector<TCheck> : MonoBehaviour where TCheck : DetectionCheck
    {
        [Header("Base Settings")]
        [FormerlySerializedAs("_originHeight")]
        [SerializeField] private float _originOffset = 0.1f;

        [Header("Base Timing")]
        [FormerlySerializedAs("_gracePeriod")]
        [FormerlySerializedAs("_wallJumpGracePeriod")]
        [FormerlySerializedAs("_barJumpGracePeriod")]
        [SerializeField] private float _jumpGracePeriod = 0.1f;

        private readonly List<TCheck> _checks = new();
        protected IReadOnlyList<TCheck> Checks => _checks;
        protected bool HasChecks => _checks.Count > 0;

        protected abstract LayerMask DefaultLayerMask { get; }

        private float _lastJumpTime;
        protected float LastJumpTime => _lastJumpTime;

        protected Vector3 RayOrigin => transform.position + Vector3.up * _originOffset;
        protected bool InJumpGrace => Time.time - _lastJumpTime < _jumpGracePeriod;

        #region Registration

        // Resolves the layer mask then registers the check.
        public virtual void AddCheck(TCheck check)
        {
            if (check.LayerMask == 0)
                check.OnLayer(DefaultLayerMask);
            RegisterCheck(check);
        }

        protected virtual void RegisterCheck(TCheck check)
        {
            RemoveCheck(check.ID);
            _checks.Add(check);
            _checks.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public virtual void RemoveCheck(string id)
        {
            _checks.RemoveAll(c => c.ID == id);

            if (_checks.Count == 0)
                ResetHits();
        }

        #endregion

        #region Tick

        // Shared tick skeleton: gate on registered checks and the jump grace period, cast all
        // checks, then hand the result to the detector via the hooks below.
        // Override entirely for fully custom behaviour.
        public virtual void Tick(Vector3 movementDirection = default)
        {
            if (!HasChecks)
            {
                ClearHit();
                return;
            }

            if (InJumpGrace)
            {
                ResetHits();
                ClearHit();
                OnGraceTick();
                return;
            }

            Vector3 normalizedMovement = movementDirection.normalized;
            CastChecks(RayOrigin, check => ResolveCastDirection(check, normalizedMovement));

            if (TryGetBestHit(out RaycastHit rawHit))
            {
                OnHit(rawHit);
            }
            else
            {
                ClearHit();
                OnMiss();
                ResetHits();
            }
        }

        public void RegisterJumpTime() => _lastJumpTime = Time.time;

        #endregion

        #region Tick Hooks

        /// <summary>Clear the detector's public Hit result.</summary>
        protected abstract void ClearHit();

        /// <summary>Build the detector's public Hit result from the raw cast hit.</summary>
        protected abstract void OnHit(RaycastHit rawHit);

        /// <summary>Called when the cast found nothing (before hits are reset).</summary>
        protected virtual void OnMiss() { }

        /// <summary>Called when a tick is suppressed by the jump grace period.</summary>
        protected virtual void OnGraceTick() { }

        /// <summary>World-space cast direction for a check. Default: the check's raw direction.</summary>
        protected virtual Vector3 ResolveCastDirection(TCheck check, Vector3 movementDirection) => check.Direction;

        #endregion

        #region Queries

        public virtual bool IsHit(string id)
        {
            for (int i = 0; i < _checks.Count; i++)
                if (_checks[i].ID == id) return _checks[i].IsHit;
            return false;
        }

        public virtual bool TryGetHit(string id, out RaycastHit hit)
        {
            for (int i = 0; i < _checks.Count; i++)
            {
                if (_checks[i].ID == id && _checks[i].IsHit)
                {
                    hit = _checks[i].Hit;
                    return true;
                }
            }
            hit = default;
            return false;
        }

        public virtual bool HasAnyHit()
        {
            for (int i = 0; i < _checks.Count; i++)
                if (_checks[i].IsHit) return true;
            return false;
        }

        #endregion

        #region Helpers

        // First hit in priority order (checks are kept sorted by priority).
        protected virtual bool TryGetBestHit(out RaycastHit bestHit)
        {
            for (int i = 0; i < _checks.Count; i++)
            {
                if (_checks[i].IsHit)
                {
                    bestHit = _checks[i].Hit;
                    return true;
                }
            }
            bestHit = default;
            return false;
        }

        protected virtual void ResetHits()
        {
            for (int i = 0; i < _checks.Count; i++)
                _checks[i].IsHit = false;
        }

        protected virtual void CastChecks(Vector3 origin, Func<TCheck, Vector3> getDirection)
        {
            for (int i = 0; i < _checks.Count; i++)
            {
                TCheck check = _checks[i];
                Vector3 dir = getDirection(check);
                Ray ray = new(origin, dir);

                bool hit;
                RaycastHit hitInfo;

                if (check.CastType == CastType.SphereCast)
                    hit = Physics.SphereCast(ray, check.Radius, out hitInfo, check.Distance, check.LayerMask, check.TriggerInteraction);
                else
                    hit = Physics.Raycast(ray, out hitInfo, check.Distance, check.LayerMask, check.TriggerInteraction);

                check.IsHit = hit;
                check.Hit = hitInfo;
            }
        }

        #endregion
    }
}
