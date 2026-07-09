using Entities.Player.Detection;
using Systems.Input;
using UnityEngine;

namespace Entities.Player
{
    public class PlayerContext : EntityContext
    {
        [Header("Input")]
        [SerializeField] private MonoBehaviour _inputProviderComponent;
        public IInputProvider InputProvider { get; private set; }

        [Header("State Machine")]
        [SerializeField] private PlayerStateMachine _stateMachine;
        public PlayerStateMachine StateMachine => _stateMachine;

        [Header("References")]
        [SerializeField] private Transform _orientation;
        public Transform Orientation => _orientation;

        [SerializeField] private Collider _collider;
        public Collider Collider => _collider;

        [SerializeField] private Rigidbody _rigidbody;
        public Rigidbody Rigidbody => _rigidbody;

        [SerializeField] private Animator _animator;
        public Animator Animator => _animator;

        [SerializeField] private Transform _playerModel;
        public Transform PlayerModel => _playerModel;

        [SerializeField] private LayerMask _defaultLayer;
        public LayerMask DefaultLayer => _defaultLayer;

        [Header("Detection")]
        [SerializeField] private GroundDetector _groundDetector;
        public GroundDetector GroundDetector => _groundDetector;

        [SerializeField] private WallDetector _wallDetector;
        public WallDetector WallDetector => _wallDetector;

        [SerializeField] private RailDetector _railDetector;
        public RailDetector RailDetector => _railDetector;

        [SerializeField] private WaterDetector _waterDetector;
        public WaterDetector WaterDetector => _waterDetector;

        [SerializeField] private BarDetector _barDetector;
        public BarDetector BarDetector => _barDetector;

        [Header("Camera")]
        [SerializeField] private Transform _cameraHolder;
        public Transform CameraHolder => _cameraHolder;

        [SerializeField] private Transform _firstPersonAnchor;
        public Transform FirstPersonAnchor => _firstPersonAnchor;

        [SerializeField] private float _cameraSensitivity = 5f;
        public float CameraSensitivity => _cameraSensitivity;

        [SerializeField] private Vector2 _cameraDistanceRange = new(3f, 5f);
        public Vector2 CameraDistanceRange => _cameraDistanceRange;

        [SerializeField] private float _cameraSmoothTime = 0.12f;
        public float CameraSmoothTime => _cameraSmoothTime;

        [SerializeField] private Vector2 _cameraPitchClamp = new(-40f, 85f);
        public Vector2 CameraPitchClamp => _cameraPitchClamp;

        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 5f;
        public float WalkSpeed => _walkSpeed;

        [SerializeField] private float _runSpeed = 8f;
        public float RunSpeed => _runSpeed;

        [SerializeField] private float _climbSpeed = 7f;
        public float ClimbSpeed => _climbSpeed;

        [SerializeField] private float _wallRunSpeed = 10f;
        public float WallRunSpeed => _wallRunSpeed;

        [SerializeField] private float _baseSlideSpeed = 6f;
        public float BaseSlideSpeed => _baseSlideSpeed;

        [SerializeField] private float _maxSlideSpeed = 15f;
        public float MaxSlideSpeed => _maxSlideSpeed;

        [SerializeField] private float _slideAcceleration = 5f;
        public float SlideAcceleration => _slideAcceleration;

        [SerializeField] private float _grindSpeed = 12f;
        public float GrindSpeed => _grindSpeed;

        [Header("Step Settings")]
        [SerializeField] private float _maxStepHeight = 0.3f;
        public float MaxStepHeight => _maxStepHeight;

        [SerializeField] private float _stepCheckDistance = 0.5f;
        public float StepCheckDistance => _stepCheckDistance;

        [SerializeField] private float _stepInset = 0.2f;
        public float StepInset => _stepInset;

        [SerializeField] private float _treadProbeForward = 0.05f;
        public float TreadProbeForward => _treadProbeForward;

        [SerializeField] private float _playerRadius = 0.5f;
        public float PlayerRadius => _playerRadius;

        [Header("Bar Settings")]
        [Tooltip("Pendulum length: how far below the bar the player hangs while swinging.")]
        [SerializeField] private float _barArmLength = 1.5f;
        public float BarArmLength => _barArmLength;

        [Tooltip("Angular acceleration (rad/s^2) added by holding the pump input.")]
        [SerializeField] private float _barPumpAcceleration = 4f;
        public float BarPumpAcceleration => _barPumpAcceleration;

        [Tooltip("How quickly an un-pumped swing settles. 0 = swings forever.")]
        [SerializeField] private float _barSwingDamping = 0.3f;
        public float BarSwingDamping => _barSwingDamping;

        [Tooltip("Cap on swing speed (rad/s).")]
        [SerializeField] private float _barMaxAngularSpeed = 6f;
        public float BarMaxAngularSpeed => _barMaxAngularSpeed;

        [Tooltip("Upward component of the launch direction when jumping off a bar.")]
        [SerializeField] private float _barLaunchBoost = 1.2f;
        public float BarLaunchBoost => _barLaunchBoost;

        [Header("Skateboard Settings")]
        [SerializeField] private float _maxSkateboardSpeed = 12f;
        public float MaxSkateboardSpeed => _maxSkateboardSpeed;

        [SerializeField] private float _brakeStrength = 10f;
        public float BrakeStrength => _brakeStrength;

        [SerializeField] private float _pushForce = 5f;
        public float PushForce => _pushForce;

        [SerializeField] private float _turnSpeed = 100f;
        public float TurnSpeed => _turnSpeed;

        [SerializeField] private float _turnGripStrength = 5f;
        public float TurnGripStrength => _turnGripStrength;

        [SerializeField] private float _skateGravityMultiplier = 1.5f;
        public float SkateGravityMultiplier => _skateGravityMultiplier;

        protected override void Awake()
        {
            base.Awake();

            InputProvider = _inputProviderComponent as IInputProvider;
            if (InputProvider == null)
                Debug.LogError("Assigned input provider does not implement IInputProvider!");
        }

        public void SetInputProvider(IInputProvider inputProvider)
        {
            InputProvider = inputProvider;
        }
    }
}