using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class GrindRail : MonoBehaviour
{
    private SplineContainer _container;
    public SplineContainer Container => _container;

    private void Awake()
    {
        _container = GetComponent<SplineContainer>();
    }

    public Spline Spline => _container[0];
}