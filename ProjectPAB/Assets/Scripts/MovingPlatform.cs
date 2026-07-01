using UnityEngine;

public interface IMovingPlatform
{
    Vector3 DeltaThisStep { get; }
}

public class MovingPlatform : MonoBehaviour, IMovingPlatform
{
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private float speed = 2f;

    private int _targetIndex;
    public Vector3 DeltaThisStep { get; private set; }

    private void Awake()
    {
        _rigidbody.isKinematic = true;

        if (_waypoints.Length > 0)
            _rigidbody.MovePosition(_waypoints[0].position);
    }

    private void FixedUpdate()
    {
        if (_waypoints.Length == 0) return;

        Vector3 before = _rigidbody.position;
        Vector3 next = Vector3.MoveTowards(before, _waypoints[_targetIndex].position, speed * Time.fixedDeltaTime);
        _rigidbody.MovePosition(next);
        DeltaThisStep = next - before;

        if (Vector3.Distance(next, _waypoints[_targetIndex].position) < 0.1f)
            _targetIndex = (_targetIndex + 1) % _waypoints.Length;
    }
}