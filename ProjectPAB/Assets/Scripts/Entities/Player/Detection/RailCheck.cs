using UnityEngine;

namespace Entities.Player.Detection
{
    public class RailCheck : DetectionCheck
    {
        public RailCheck(string id, Vector3 direction, float distance, int priority, CastType type, float radius = 0, LayerMask layerMask = default, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
           : base(id, direction, distance, priority, type, radius, layerMask, triggerInteraction)
        {

        }
    }
}