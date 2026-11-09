using UnityEngine;

/// <summary>
/// Attach this to any bar GameObject the player can grab.
/// Provides precise dimensions and local-space tracking so the player
/// follows the bar correctly when it moves (e.g. on a moving platform).
/// The bar's length runs along the GameObject's local X axis (transform.right).
/// </summary>
public class BarAttachment : MonoBehaviour
{
    [Tooltip("Half the length of the bar along its local X axis.")]
    [SerializeField] private float _halfLength = 1f;
    public float HalfLength => _halfLength;

    [Tooltip("Radius of the bar cross-section. Used to place the player flush on top.")]
    [SerializeField] private float _radius = 0.1f;
    public float Radius => _radius;

    /// <summary>The bar's axis direction in world space (always transform.right).</summary>
    public Vector3 Axis => transform.right;

    /// <summary>
    /// Returns the closest point on the bar's center line to <paramref name="worldPos"/>,
    /// clamped to the bar's endpoints.
    /// </summary>
    public Vector3 GetClosestPointOnAxis(Vector3 worldPos)
    {
        float t = WorldToLocalT(worldPos);
        return LocalTToWorld(t);
    }

    /// <summary>
    /// Projects <paramref name="worldPos"/> onto the bar axis and returns the signed distance
    /// from the bar center, clamped to [-HalfLength, HalfLength].
    /// Store this at grab time and pass it to <see cref="LocalTToWorld"/> every frame
    /// to follow a moving bar.
    /// </summary>
    public float WorldToLocalT(Vector3 worldPos)
    {
        float t = Vector3.Dot(worldPos - transform.position, transform.right);
        return Mathf.Clamp(t, -_halfLength, _halfLength);
    }

    /// <summary>
    /// Converts a stored local-T value back to a world-space point on the bar center line.
    /// Call every FixedUpdate to keep the pivot in sync with a moving bar.
    /// </summary>
    public Vector3 LocalTToWorld(float t)
    {
        return transform.position + transform.right * t;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position;
        Vector3 axis   = transform.right;
        Vector3 left   = center - axis * _halfLength;
        Vector3 right  = center + axis * _halfLength;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(left, right);
        Gizmos.DrawWireSphere(left,  _radius);
        Gizmos.DrawWireSphere(right, _radius);
        Gizmos.DrawWireSphere(center, _radius);
    }
#endif
}
