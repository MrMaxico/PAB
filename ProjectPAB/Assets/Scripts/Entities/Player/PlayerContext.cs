using Entities.Base;
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
        #region References

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

        #endregion

        [Header("Detection")]
        #region Detection

        [SerializeField] private LayerMask _groundLayer = 1;   // Default
        public LayerMask GroundLayer => _groundLayer;

        [SerializeField] private LayerMask _wallLayer = 1;     // Default
        public LayerMask WallLayer => _wallLayer;

        [SerializeField] private LayerMask _railLayer = 64;    // Rail
        public LayerMask RailLayer => _railLayer;

        [SerializeField] private LayerMask _barLayer = 7;    // Bar
        public LayerMask BarLayer => _barLayer;

        [Tooltip("Height above the body position that ground and rail probes start from.")]
        [SerializeField] private float _groundProbeHeight = 0.1f;
        public float GroundProbeHeight => _groundProbeHeight;

        [Tooltip("Height above the body position that wall probes start from.")]
        [SerializeField] private float _wallProbeHeight = 0.5f;
        public float WallProbeHeight => _wallProbeHeight;

        [Tooltip("Height above the body position that bar probes start from.")]
        [SerializeField] private float _barProbeHeight = 0.5f;
        public float BarProbeHeight => _barProbeHeight;

        [SerializeField] private float _maxSlopeAngle = 45f;
        public float MaxSlopeAngle => _maxSlopeAngle;

        [Tooltip("How long after last touching the ground a jump is still allowed.")]
        [SerializeField] private float _coyoteTime = 0.5f;
        public float CoyoteTime => _coyoteTime;

        [Tooltip("After jumping off a surface, ignore that kind of surface for this long so you don't instantly re-attach.")]
        [SerializeField] private float _groundJumpGrace = 0.3f;
        public float GroundJumpGrace => _groundJumpGrace;

        [SerializeField] private float _groundBarGrace = 0.3f;
        public float GroundBarGrace => _groundBarGrace;

        [SerializeField] private float _wallJumpGrace = 0.1f;
        public float WallJumpGrace => _wallJumpGrace;

        [SerializeField] private float _railJumpGrace = 0.3f;
        public float RailJumpGrace => _railJumpGrace;

        [SerializeField] private float _barJumpGrace = 0.7f;
        public float BarJumpGrace => _barJumpGrace;

        [SerializeField] private float _barAirGrace = 0.7f;
        public float BarAirborneGrace => _barAirGrace;

        [SerializeField] private float _barGroundedGrace = 0.3f;
        public float BarGroundedGrace => _barGroundedGrace;

        [Tooltip("Maximum angle (degrees) between the player-to-bar vector and Vector3.up that still counts as grabbing from above. 0 = must be directly above; 90 = side entry also counts.")]
        [SerializeField][Range(0f, 90f)] private float _barTopEntryAngle = 60f;
        public float BarTopEntryAngle => _barTopEntryAngle;


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

        #endregion

        [Header("Movement Settings")]
        #region Movement Settings

        [SerializeField] private float _walkSpeed = 5f;
        public float WalkSpeed => _walkSpeed;

        [SerializeField] private float _runSpeed = 8f;
        public float RunSpeed => _runSpeed;

        [SerializeField] private float _climbSpeed = 7f;
        public float ClimbSpeed => _climbSpeed;

        [SerializeField] private float _wallRunSpeed = 10f;
        public float WallRunSpeed => _wallRunSpeed;

        [SerializeField] private float _grindSpeed = 12f;
        public float GrindSpeed => _grindSpeed;

        [SerializeField] private float _airStrafeForce = 4f;
        public float AirStrafeForce => _airStrafeForce;

        [SerializeField] private float _baseAirStrafeSpeed = 7f;
        public float BaseAirStrafeSpeed => _baseAirStrafeSpeed;

        [SerializeField] private float _airStrafeGain = 1.5f;
        public float AirStrafeGain => _airStrafeGain;

        [SerializeField] private float _maxAirStrafeSpeed = 13f;
        public float MaxAirStrafeSpeed => _maxAirStrafeSpeed;

        [Tooltip("Max time between leaving the air and re-entering it for a hop chain to continue. Buffered/held jump re-jumps within ~1 frame (0.02s), so this only needs a little slack.")]
        [SerializeField] private float _strafeHopWindow = 0.1f;
        public float StrafeHopWindow => _strafeHopWindow;


        #endregion

        [Header("Step Settings")]
        #region Step Settings

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

        #endregion

        [Header("Swing Settings")]
        #region Swing Settings

        [SerializeField] private float _swingLength = 1.6f;
        public float SwingLength => _swingLength;

        [SerializeField] private float _swingPumpStrength = 6f;
        public float SwingPumpStrength => _swingPumpStrength;

        [SerializeField] private float _swingDamping = 0.35f;
        public float SwingDamping => _swingDamping;

        [SerializeField] private float _swingGravity = 9.81f;
        public float SwingGravity => _swingGravity;

        [SerializeField] private float _swingReleaseBoost = 2f;
        public float SwingReleaseBoost => _swingReleaseBoost;

        #endregion

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