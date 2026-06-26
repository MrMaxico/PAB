using UnityEngine;

namespace Entities.Player.Detection
{
    public enum CastType
    {
        Raycast,
        SphereCast,
    }

    public abstract class DetectionCheck
    {
        public string Id { get; }

        public Vector3 Direction { get; }

        public CastType Type { get; }

        public float Distance { get; }

        public float Radius { get; }

        /// <summary>Lower runs first and wins priority ties.</summary>
        public int Priority { get; }

        /// <summary>Layer mask used for this specific check.</summary>
        public LayerMask LayerMask { get; }

        /// <summary>Whether this check detects trigger colliders.</summary>
        public QueryTriggerInteraction TriggerInteraction { get; }

        public bool IsHit { get; set; }
        public RaycastHit HitInfo { get; set; }

        public bool UseMovementDirection { get; }

        protected DetectionCheck(string id, Vector3 direction, float distance, int priority, CastType type, float radius = 0, LayerMask layerMask = default, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore, bool useMovementDirection = false)
        {
            Id = id;
            Direction = direction;
            Type = type;
            Distance = distance;
            Radius = radius;
            Priority = priority;
            LayerMask = layerMask;
            TriggerInteraction = triggerInteraction;
            UseMovementDirection = useMovementDirection;
        }
    }
}