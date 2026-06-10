using System.Collections.Generic;
using UnityEngine;

namespace Entities.Player.Detection
{
    public abstract class BaseDetector<TCheck> : MonoBehaviour where TCheck : DetectionCheck
    {
        // --- Registered checks (sorted by priority) ---
        private readonly List<TCheck> _checks = new List<TCheck>();
        protected IReadOnlyList<TCheck> Checks => _checks;

        /// <summary>The default layer mask for this detector. Subclasses provide via serialized field.</summary>
        protected abstract LayerMask DefaultLayerMask { get; }

        // ─── Registration ─── \\

        /// <summary>
        /// Insert a check into the sorted list. Removes any existing check with the same id first.
        /// Layer mask should already be resolved by the subclass before calling this.
        /// </summary>
        protected void RegisterCheck(TCheck check)
        {
            RemoveCheck(check.Id);
            _checks.Add(check);
            _checks.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public void RemoveCheck(string id)
        {
            _checks.RemoveAll(c => c.Id == id);
        }

        /// <summary>Query whether a specific registered check is currently hitting.</summary>
        public bool IsHit(string id)
        {
            for (int i = 0; i < _checks.Count; i++)
            {
                if (_checks[i].Id == id) return _checks[i].IsHit;
            }
            return false;
        }

        /// <summary>Get the hit info for a specific check, if it hit.</summary>
        public bool TryGetHit(string id, out RaycastHit hit)
        {
            for (int i = 0; i < _checks.Count; i++)
            {
                if (_checks[i].Id == id && _checks[i].IsHit)
                {
                    hit = _checks[i].HitInfo;
                    return true;
                }
            }
            hit = default;
            return false;
        }

        // ─── Shared helpers ─── \\

        public bool HasAnyHit()
        {
            for (int i = 0; i < _checks.Count; i++)
            {
                if (_checks[i].IsHit) return true;
            }
            return false;
        }

        protected void ClearChecks()
        {
            for (int i = 0; i < _checks.Count; i++)
                _checks[i].IsHit = false;
        }

        /// <summary>Resolves a layer mask — returns <see cref="DefaultLayerMask"/> when the given mask is 0.</summary>
        protected LayerMask ResolveLayerMask(LayerMask layerMask)
        {
            return layerMask == 0 ? DefaultLayerMask : layerMask;
        }
    }
}