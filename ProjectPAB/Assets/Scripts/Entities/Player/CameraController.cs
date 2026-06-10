using Systems.Input;
using UnityEngine;

namespace Entities.Player
{
    public class CameraController : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private PlayerContext _playerContext;

        private PlayerStateMachine StateMachine => _playerContext.StateMachine;
        private Transform Target => _playerContext.transform;
        public Transform Orientation => _playerContext.Orientation;
        private IInputProvider _inputProvider => _playerContext.InputProvider;

        private float _rotationX;
        private float _rotationY;
        private Vector3 _currentVelocity;

        private void Update()
        {
            _rotationX -= _inputProvider.GetMouseInput().y * _playerContext.CameraSensitivity;
            _rotationY += _inputProvider.GetMouseInput().x * _playerContext.CameraSensitivity;
            _rotationX = Mathf.Clamp(_rotationX, _playerContext.CameraPitchClamp.x, _playerContext.CameraPitchClamp.y);

            transform.rotation = Quaternion.Euler(_rotationX, _rotationY, 0);
            Orientation.rotation = Quaternion.Euler(0, _rotationY, 0);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(Orientation.position, Orientation.forward + Orientation.position);
        }
    }
}