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

        public Vector3 Point => RawHit.point;
        public Vector3 Normal => RawHit.normal;
        public float Distance => RawHit.distance;

        public GameObject GameObject => RawHit.collider != null ? RawHit.collider.gameObject : null;
        public Transform Transform => RawHit.transform;
        public Collider Collider => RawHit.collider;
        public Rigidbody Rigidbody => RawHit.rigidbody;

        public Vector3 Velocity => Rigidbody != null ? Rigidbody.linearVelocity : Vector3.zero;
        public Vector3 SurfaceVelocity => Rigidbody != null ? Rigidbody.linearVelocity : Vector3.zero;
        public Vector3 SurfaceAngularVelocity => Rigidbody != null ? Rigidbody.angularVelocity : Vector3.zero;

        public LayerMask Layer => 1 << RawHit.collider.gameObject.layer;

        public IMovingPlatform Platform => RawHit.collider != null ? RawHit.collider.GetComponentInParent<IMovingPlatform>() : null;

        public float SlopeAngle { get; }
        public bool IsSloped => SlopeAngle > 0.1f;

        public Vector3 Forward { get; }

        public DetectionHit(RaycastHit rawHit, float slopeAngle = 0f, Vector3 forward = default)
        {
            IsHit = true;
            RawHit = rawHit;
            SlopeAngle = slopeAngle;
            Forward = forward;
        }
    }

    public enum BarApproach
    {
        Above,
        Below,
        Side,
    }

    public enum BarOrientation
    {
        Horizontal,
        Diagonal,
        Vertical,
    }

    public readonly struct BarHit
    {
        public static readonly BarHit None = default;

        public bool IsHit { get; }
        public RaycastHit RawHit { get; }

        /// <summary>Closest point on the bar's center axis to the player — the swing pivot.</summary>
        public Vector3 GrabPoint { get; }

        /// <summary>Normalized bar axis. The player swings in the plane perpendicular to this.</summary>
        public Vector3 Axis { get; }

        /// <summary>Normalized direction from the grab point toward the player at detection time.</summary>
        public Vector3 ApproachDirection { get; }

        public BarApproach Approach { get; }
        public BarOrientation Orientation { get; }

        public Vector3 Point => RawHit.point;
        public Vector3 Normal => RawHit.normal;
        public float Distance => RawHit.distance;

        public GameObject GameObject => RawHit.collider != null ? RawHit.collider.gameObject : null;
        public Transform Transform => RawHit.transform;
        public Collider Collider => RawHit.collider;
        public Rigidbody Rigidbody => RawHit.rigidbody;

        public Vector3 Velocity => Rigidbody != null ? Rigidbody.linearVelocity : Vector3.zero;

        public IMovingPlatform Platform => RawHit.collider != null ? RawHit.collider.GetComponentInParent<IMovingPlatform>() : null;

        public BarHit(RaycastHit rawHit, Vector3 grabPoint, Vector3 axis, Vector3 approachDirection)
        {
            IsHit = true;
            RawHit = rawHit;
            GrabPoint = grabPoint;
            Axis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.right;

            ApproachDirection = approachDirection.sqrMagnitude > 0.0001f
                ? approachDirection.normalized
                : Vector3.down;

            float upDot = Vector3.Dot(ApproachDirection, Vector3.up);
            Approach = upDot > 0.5f ? BarApproach.Above
                     : upDot < -0.5f ? BarApproach.Below
                     : BarApproach.Side;

            Orientation = ClassifyOrientation(Axis);
        }

        public static BarOrientation ClassifyOrientation(Vector3 axis)
        {
            float upDot = Mathf.Abs(Vector3.Dot(axis.normalized, Vector3.up));
            return upDot > 0.85f ? BarOrientation.Vertical
                 : upDot < 0.35f ? BarOrientation.Horizontal
                 : BarOrientation.Diagonal;
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

        #region Static Factories

        public static DetectionCheck Ray(string id, Vector3 direction, float distance, int priority = 0) =>
            new DetectionCheck(id).InDirection(direction).WithDistance(distance).AtPriority(priority);

        public static DetectionCheck Sphere(string id, Vector3 direction, float distance, float radius, int priority = 0) =>
            new DetectionCheck(id).InDirection(direction).WithDistance(distance).AsSphere(radius).AtPriority(priority);

        #endregion

        #region Fluent API

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

        #endregion
    }
}