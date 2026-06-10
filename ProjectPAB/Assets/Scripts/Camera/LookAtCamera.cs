using TMPro;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private Transform _target;

    private void LateUpdate()
    {
        if (_target != null)
        {
            transform.LookAt(_target);
        }
    }
}
