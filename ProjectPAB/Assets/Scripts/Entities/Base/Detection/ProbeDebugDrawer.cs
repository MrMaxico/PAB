using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Entities.Base.Detection
{
    /// <summary>
    /// Draws every probe on this body that cast during the latest physics step.
    /// Probes that weren't read don't cast, so they don't show up here either.
    /// Add it to the GameObject that has the Rigidbody.
    /// </summary>
    public class ProbeDebugDrawer : MonoBehaviour
    {
        [SerializeField] private bool _draw = true;
        [SerializeField] private bool _drawLabels = true;
        [SerializeField] private string _nameFilter = "";
        [SerializeField] private Color _hitColor = Color.green;
        [SerializeField] private Color _missColor = Color.red;
        [SerializeField] private Color _suppressedColor = Color.gray;

        private Rigidbody _body;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_draw || !Application.isPlaying) return;
            if (_body == null && !TryGetComponent(out _body)) return;

            foreach (Probe probe in Probe.EvaluatedThisStep)
            {
                if (probe.Body != _body) continue;
                if (!string.IsNullOrEmpty(_nameFilter) && !probe.DebugName.Contains(_nameFilter)) continue;

                bool hit = probe.DebugIsHit;
                Gizmos.color = hit ? _hitColor : _missColor;

                // Overlap sphere visualization
                if (probe.Shape == ProbeShape.OverlapSphere)
                {
                    Gizmos.DrawWireSphere(probe.LastOrigin, probe.Radius);
#if UNITY_EDITOR
                    if (_drawLabels)
                        Handles.Label(probe.LastOrigin + Vector3.up * 0.15f, $"{probe.DebugName} ({probe.Shape})");
#endif
                    continue;
                }

                // Ray / SphereCast visualization
                Vector3 origin = probe.LastOrigin;
                Vector3 dir = probe.LastDirection;

                if (dir == Vector3.zero)
                {
                    Gizmos.color = _suppressedColor;
                    Gizmos.DrawWireSphere(origin, 0.05f);
                    continue;
                }

                float length = hit ? probe.DebugHit.distance : probe.Distance;
                Gizmos.DrawRay(origin, dir * length);

                if (probe.Shape == ProbeShape.Sphere)
                {
                    Gizmos.DrawWireSphere(origin, probe.Radius);
                    Gizmos.DrawWireSphere(origin + dir * length, probe.Radius);
                }

                if (hit)
                {
                    Gizmos.DrawSphere(probe.DebugHit.point, 0.04f);
                    Gizmos.DrawRay(probe.DebugHit.point, probe.DebugHit.normal * 0.3f);
                }

#if UNITY_EDITOR
                if (_drawLabels)
                    Handles.Label(origin + dir * Mathf.Min(length, 0.3f), $"{probe.DebugName} ({probe.Shape})");
#endif
            }
        }
#endif
    }
}
