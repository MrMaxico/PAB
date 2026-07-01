using UnityEngine;

namespace Entities.Player.Detection
{
    public enum CastType
    {
        Raycast,
        SphereCast,
    }

    public readonly struct DetectionHit
    {
        public static readonly DetectionHit None = default;

        public bool IsHit { get; }
        public RaycastHit RawHit { get; }

        // ─── RaycastHit passthrough ───
        public Vector3 Point => RawHit.point;
        public Vector3 Normal => RawHit.normal;
        public float Distance => RawHit.distance;
        public Collider Collider => RawHit.collider;
        public GameObject GameObject => RawHit.collider.gameObject;
        public Transform Transform => RawHit.transform;
        public Rigidbody Rigidbody => RawHit.rigidbody;
        public LayerMask Layer => 1 << RawHit.collider.gameObject.layer;

        // ─── Surface motion ───
        public Vector3 SurfaceVelocity => Rigidbody != null ? Rigidbody.linearVelocity : Vector3.zero;
        public Vector3 SurfaceAngularVelocity => Rigidbody != null ? Rigidbody.angularVelocity : Vector3.zero;

        // ─── Slope ───
        public float SlopeAngle { get; }
        public bool IsSloped => SlopeAngle > 0.1f;

        // ─── Directional ───
        // Wall: direction along the wall matching movement. Ground: unused (zero).
        public Vector3 Forward { get; }

        public DetectionHit(RaycastHit rawHit, float slopeAngle = 0f, Vector3 forward = default)
        {
            IsHit = true;
            RawHit = rawHit;
            SlopeAngle = slopeAngle;
            Forward = forward;
        }
    }

    public class DetectionCheck
    {
        public string ID { get; }
        public Vector3 Direction { get; private set; }
        public CastType CastType { get; private set; }
        public float Distance { get; private set; }
        public float Radius { get; private set; }
        public int Priority { get; private set; }
        public LayerMask LayerMask { get; private set; }
        public QueryTriggerInteraction TriggerInteraction { get; private set; }

        public bool IsHit { get; set; }
        public RaycastHit Hit { get; set; }

        public DetectionCheck(string id)
        {
            ID = id;
            this.CastType = CastType.Raycast;
            TriggerInteraction = QueryTriggerInteraction.Ignore;
        }

        // ─── Static factories ───

        public static DetectionCheck Ray(string id, Vector3 direction, float distance, int priority = 0) =>
            new DetectionCheck(id).InDirection(direction).WithDistance(distance).AtPriority(priority);

        public static DetectionCheck Sphere(string id, Vector3 direction, float distance, float radius, int priority = 0) =>
            new DetectionCheck(id).InDirection(direction).WithDistance(distance).AsSphere(radius).AtPriority(priority);

        // ─── Fluent API ───

        public DetectionCheck InDirection(Vector3 direction) { Direction = direction; return this; }
        public DetectionCheck WithDistance(float distance) { Distance = distance; return this; }
        public DetectionCheck AtPriority(int priority) { Priority = priority; return this; }
        public DetectionCheck OnLayer(LayerMask layer) { LayerMask = layer; return this; }
        public DetectionCheck IncludeTriggers() { TriggerInteraction = QueryTriggerInteraction.Collide; return this; }

        public DetectionCheck AsSphere(float radius)
        {
            CastType = CastType.SphereCast;
            Radius = radius;
            return this;
        }
    }
}