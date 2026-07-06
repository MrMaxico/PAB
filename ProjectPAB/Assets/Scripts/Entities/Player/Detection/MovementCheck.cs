using UnityEngine;

namespace Entities.Player.Detection
{
    public class MovementCheck : DetectionCheck
    {
        public bool UseMovementDirection { get; }

        public MovementCheck(string id, bool useMovementDirection = false) : base(id)
        {
            UseMovementDirection = useMovementDirection;
        }

        // ─── Static factories ───

        public static new MovementCheck Ray(string id, Vector3 direction, float distance, int priority = 0)
        {
            var check = new MovementCheck(id);
            check.InDirection(direction).WithDistance(distance).AtPriority(priority);
            return check;
        }

        public static new MovementCheck Sphere(string id, Vector3 direction, float distance, float radius, int priority = 0)
        {
            var check = new MovementCheck(id);
            check.InDirection(direction).WithDistance(distance).AsSphere(radius).AtPriority(priority);
            return check;
        }

        public static MovementCheck Movement(string id, float distance, int priority = 0)
        {
            var check = new MovementCheck(id, useMovementDirection: true);
            check.WithDistance(distance).AtPriority(priority);
            return check;
        }
    }
}
