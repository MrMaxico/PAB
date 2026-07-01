using System;
using System.Collections.Generic;
using UnityEngine;

namespace Entities.Player.Detection
{
    public abstract class BaseDetector<TCheck> : MonoBehaviour where TCheck : DetectionCheck
    {
        private readonly List<TCheck> _checks = new();
        protected IReadOnlyList<TCheck> Checks => _checks;

        protected abstract LayerMask DefaultLayerMask { get; }

        // ─── Registration ───

        // Resolves the layer mask then registers the check.
        public void AddCheck(TCheck check)
        {
            if (check.LayerMask == 0)
                check.OnLayer(DefaultLayerMask);
            RegisterCheck(check);
        }

        protected void RegisterCheck(TCheck check)
        {
            RemoveCheck(check.ID);
            _checks.Add(check);
            _checks.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public void RemoveCheck(string id) => _checks.RemoveAll(c => c.ID == id);

        // ─── Queries ───

        public bool IsHit(string id)
        {
            for (int i = 0; i < _checks.Count; i++)
                if (_checks[i].ID == id) return _checks[i].IsHit;
            return false;
        }

        public bool TryGetHit(string id, out RaycastHit hit)
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

        public bool HasAnyHit()
        {
            for (int i = 0; i < _checks.Count; i++)
                if (_checks[i].IsHit) return true;
            return false;
        }

        // ─── Helpers ───

        protected void ResetHits()
        {
            for (int i = 0; i < _checks.Count; i++)
                _checks[i].IsHit = false;
        }

        // Runs every check from origin, using getDirection to resolve the cast direction per check.
        protected void CastChecks(Vector3 origin, Func<TCheck, Vector3> getDirection)
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
    }
}