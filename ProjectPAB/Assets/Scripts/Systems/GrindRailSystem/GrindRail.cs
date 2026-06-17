using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GrindRail : MonoBehaviour
{
    private SplineContainer _container;
    public SplineContainer Container => _container;
    public Spline Spline => _container != null && _container.Splines.Count > 0 ? _container[0] : null;

    [Header("Tube Geometry")]
    [Tooltip("Number of cross-sections per meter. Lower for performance, higher for smooth curves.")]
    [SerializeField] private int _segmentsPerMeter = 4;
    [Tooltip("Radius of the generated tube.")]
    [SerializeField] private float _railRadius = 0.2f;
    [Tooltip("How circular the tube is. Higher numbers (e.g., 16-24) create a smooth cylinder; low numbers (e.g., 3-6) create polygons.")]
    [Range(3, 32)]
    [SerializeField] private int _radialSegments = 12;
    [SerializeField] private Material _railMaterial;

    [Header("Positioning Offset")]
    [Tooltip("Offsets the entire tube mesh relative to the spline path. X = Left/Right, Y = Up/Down.")]
    [SerializeField] private Vector3 _hitboxOffset = Vector3.zero;

    [Header("Physics Settings")]
    [SerializeField] private bool _generateCollider = true;
    [SerializeField] private bool _isTrigger = true;

    private void Awake()
    {
        _container = GetComponent<SplineContainer>();
    }

    [ContextMenu("Generate Continuous Tube Mesh")]
    public void GenerateMeshRail()
    {
        _container = GetComponent<SplineContainer>();
        if (Spline == null || Spline.Count < 2)
        {
            Debug.LogWarning("Cannot generate rail: Spline is missing or doesn't have enough points.");
            return;
        }

        // 1. Get or Add required rendering components on THIS GameObject
        if (!TryGetComponent(out MeshFilter meshFilter))
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if (!TryGetComponent(out MeshRenderer meshRenderer))
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        meshRenderer.material = _railMaterial;

        // Clean up previous mesh data safely before calculating new geometry
        ClearExistingRail();

        // 2. Calculate Math Segments
        float splineLength = Spline.GetLength();
        int totalSegments = Mathf.Max(2, Mathf.CeilToInt(splineLength * _segmentsPerMeter));
        int totalRings = totalSegments + 1;
        float step = 1f / totalSegments;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // 3. Generate Rings of Vertices (Circular cross-sections) along the spline
        for (int i = 0; i < totalRings; i++)
        {
            float t = i * step;

            Vector3 position = (Vector3)Spline.EvaluatePosition(t);
            Vector3 direction = ((Vector3)Spline.EvaluateTangent(t)).normalized;
            Vector3 up = ((Vector3)Spline.EvaluateUpVector(t)).normalized;

            Vector3 right = Vector3.Cross(up, direction).normalized;
            up = Vector3.Cross(direction, right).normalized; // Recalculate up to guarantee right angles

            Quaternion rotation = Quaternion.identity;
            if (direction != Vector3.zero)
            {
                rotation = Quaternion.LookRotation(direction, up);
            }

            Vector3 localOffset = rotation * _hitboxOffset;
            Vector3 ringCenter = position + localOffset;

            // Generate a circle of points at this center
            for (int r = 0; r < _radialSegments; r++)
            {
                float angle = (float)r / _radialSegments * Mathf.PI * 2f;

                Vector3 radialVec = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * _railRadius;
                vertices.Add(ringCenter + radialVec);
            }
        }

        // 4. Connect the Rings with Triangles (Stitching the tube faces)
        for (int s = 0; s < totalSegments; s++)
        {
            for (int r = 0; r < _radialSegments; r++)
            {
                int currentRingPoint = s * _radialSegments + r;
                int nextRingPoint = (s + 1) * _radialSegments + r;
                int nextRadialPoint = s * _radialSegments + (r + 1) % _radialSegments;
                int nextRingNextRadialPoint = (s + 1) * _radialSegments + (r + 1) % _radialSegments;

                // Triangle 1
                triangles.Add(currentRingPoint);
                triangles.Add(nextRingNextRadialPoint);
                triangles.Add(nextRingPoint);

                // Triangle 2
                triangles.Add(currentRingPoint);
                triangles.Add(nextRadialPoint);
                triangles.Add(nextRingNextRadialPoint);
            }
        }

        // Optional: Cap the start and end of the tube if it's open-ended
        if (!Spline.Closed)
        {
            // Start Cap
            int startCapCenterIndex = vertices.Count;
            Vector3 sDirection = ((Vector3)Spline.EvaluateTangent(0f)).normalized;
            Vector3 sUp = ((Vector3)Spline.EvaluateUpVector(0f)).normalized;
            Vector3 startCenter = (Vector3)Spline.EvaluatePosition(0f) + (Quaternion.LookRotation(sDirection, sUp) * _hitboxOffset);
            vertices.Add(startCenter);

            for (int r = 0; r < _radialSegments; r++)
            {
                int nextR = (r + 1) % _radialSegments;
                triangles.Add(startCapCenterIndex);
                triangles.Add(nextR);
                triangles.Add(r);
            }

            // End Cap
            int lastRingStart = (totalRings - 1) * _radialSegments;
            int endCapCenterIndex = vertices.Count;
            Vector3 eDirection = ((Vector3)Spline.EvaluateTangent(1f)).normalized;
            Vector3 eUp = ((Vector3)Spline.EvaluateUpVector(1f)).normalized;
            Vector3 endCenter = (Vector3)Spline.EvaluatePosition(1f) + (Quaternion.LookRotation(eDirection, eUp) * _hitboxOffset);
            vertices.Add(endCenter);

            for (int r = 0; r < _radialSegments; r++)
            {
                int nextR = (r + 1) % _radialSegments;
                triangles.Add(endCapCenterIndex);
                triangles.Add(lastRingStart + r);
                triangles.Add(lastRingStart + nextR);
            }
        }

        // 5. Assign data to Mesh
        Mesh continuousMesh = new Mesh();
        continuousMesh.name = "ContinuousTubeMesh";
        continuousMesh.vertices = vertices.ToArray();
        continuousMesh.triangles = triangles.ToArray();

        continuousMesh.RecalculateNormals();
        continuousMesh.RecalculateBounds();

        meshFilter.sharedMesh = continuousMesh;

        // 6. Generate continuous Hitbox on the same object
        if (_generateCollider)
        {
            if (!TryGetComponent(out MeshCollider meshCollider))
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
            meshCollider.sharedMesh = continuousMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = _isTrigger;
        }

        Debug.Log($"Successfully generated a single-object tube mesh with {totalSegments} segments!");
    }

    [ContextMenu("Clear Tube Mesh")]
    public void ClearExistingRail()
    {
        // Clear MeshFilter
        if (TryGetComponent(out MeshFilter meshFilter))
        {
            if (meshFilter.sharedMesh != null)
            {
                // In edit mode, we destroy immediate to prevent memory leaks
                if (!Application.isPlaying)
                {
                    DestroyImmediate(meshFilter.sharedMesh);
                }
                else
                {
                    Destroy(meshFilter.sharedMesh);
                }
                meshFilter.sharedMesh = null;
            }
        }

        // Remove Collider if it exists or if _generateCollider was turned off
        if (TryGetComponent(out MeshCollider meshCollider))
        {
            if (!Application.isPlaying)
            {
                DestroyImmediate(meshCollider);
            }
            else
            {
                Destroy(meshCollider);
            }
        }
    }
}