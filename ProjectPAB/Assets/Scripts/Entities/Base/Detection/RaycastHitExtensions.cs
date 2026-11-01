using UnityEngine;

namespace Entities.Base.Detection
{
    /// <summary>
    /// Surface information derived from a hit, usable on any probe result.
    /// </summary>
    public static class RaycastHitExtensions
    {
        /// <summary>Angle between the surface normal and world up, in degrees.</summary>
        public static float SlopeAngle(this RaycastHit hit) => Vector3.Angle(Vector3.up, hit.normal);

        public static bool IsSloped(this RaycastHit hit) => hit.SlopeAngle() > 0.1f;

        /// <summary>The moving platform the hit collider belongs to, if any.</summary>
        public static IMovingPlatform Platform(this RaycastHit hit) =>
            hit.collider != null ? hit.collider.GetComponentInParent<IMovingPlatform>() : null;

        /// <summary>Velocity of the hit rigidbody, or zero if it has none.</summary>
        public static Vector3 SurfaceVelocity(this RaycastHit hit) =>
            hit.rigidbody != null ? hit.rigidbody.linearVelocity : Vector3.zero;

        /// <summary>A mask containing only the hit object's layer.</summary>
        public static LayerMask LayerMaskOf(this RaycastHit hit) =>
            hit.collider != null ? 1 << hit.collider.gameObject.layer : 0;

        /// <summary>
        /// Direction along a wall: the velocity projected onto the wall plane, or
        /// <paramref name="fallbackForward"/> projected onto it when barely moving.
        /// </summary>
        public static Vector3 WallForward(this RaycastHit hit, Vector3 velocity, Vector3 fallbackForward)
        {
            Vector3 velocityOnWall = Vector3.ProjectOnPlane(velocity, hit.normal);
            return velocityOnWall.sqrMagnitude > 0.01f
                ? velocityOnWall.normalized
                : Vector3.ProjectOnPlane(fallbackForward, hit.normal).normalized;
        }
    }
}
