using UnityEngine;

namespace Entities.Player.Detection
{
    public class WallCheck : DetectionCheck
    {
        public bool UseMovementDirection { get; }

        public WallCheck(string id, bool useMovementDirection = false) : base(id)
        {
            UseMovementDirection = useMovementDirection;
        }

        // ─── Static factories ───

        public static new WallCheck Ray(string id, Vector3 direction, float distance, int priority = 0)
        {
            var check = new WallCheck(id);
            check.InDirection(direction).WithDistance(distance).AtPriority(priority);
            return check;
        }

        public static new WallCheck Sphere(string id, Vector3 direction, float distance, float radius, int priority = 0)
        {
            var check = new WallCheck(id);
            check.InDirection(direction).WithDistance(distance).AsSphere(radius).AtPriority(priority);
            return check;
        }

        public static WallCheck Movement(string id, float distance, int priority = 0)
        {
            var check = new WallCheck(id, useMovementDirection: true);
            check.WithDistance(distance).AtPriority(priority);
            return check;
        }
    }
}
