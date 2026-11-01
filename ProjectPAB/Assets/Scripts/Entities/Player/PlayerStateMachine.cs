using Entities.Base;
using Entities.Base.Detection;
using Entities.Player.States;
using Systems.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Entities.Player
{
    [RequireComponent(typeof(PlayerContext))]
    public class PlayerStateMachine : EntityStateMachine
    {
        [Header("Debug")]
        [SerializeField] private bool _doDebug = false;
        public bool DoDebug => _doDebug;

        [SerializeField] private PlayerContext _playerContext;
        public PlayerContext PlayerContext
        {
            get => _playerContext; private set { _playerContext = value; }
        }

        public IInputProvider InputProvider => PlayerContext.InputProvider;
        public Transform PlayerModel => PlayerContext.PlayerModel;

        public Quaternion SmoothModelRotation
        {
            get => PlayerModel.rotation;
            set => PlayerModel.rotation = value;
        }
        public Quaternion SnapModelRotation
        {
            set
            {
                RotationSnapped = true;
                SmoothModelRotation = value;
            }
        }

        public Transform Orientation => PlayerContext.Orientation;
        public Transform Transform => PlayerContext.transform;
        public Rigidbody Rigidbody => PlayerContext.Rigidbody;
        public Collider Collider => PlayerContext.Collider;
        public Animator Animator => PlayerContext.Animator;

        // Camera proxies
        public Transform CameraHolder => PlayerContext.CameraHolder;
        public Transform FirstPersonAnchor => PlayerContext.FirstPersonAnchor;

        #region Detection

        // Shared suppression timers: suppressing one makes every probe of that kind report no hit
        // for a moment (e.g. ignore the ground right after jumping off it).
        public ProbeGate GroundGate { get; } = new();
        public ProbeGate WallGate { get; } = new();
        public ProbeGate RailGate { get; } = new();
        public ProbeGate BarGate { get; } = new();

        /// <summary>The active root state's ground probe. Root states set this in EnterState so sub-states read the same probe.</summary>
        public Probe Ground { get; set; }

        /// <summary>The active root state's wall probes in priority order. Root states set this in EnterState.</summary>
        public ProbeGroup Walls { get; set; } = ProbeGroup.Empty;

        /// <summary>The active root state's rail probe. Root states set this in EnterState.</summary>
        public Probe Bar { get; set; }

        /// <summary>Last time the Grounded state's ground probe hit. Drives coyote time.</summary>
        public float LastGroundedTime { get; set; } = float.NegativeInfinity;
        public bool InCoyoteTime => Time.time - LastGroundedTime < PlayerContext.CoyoteTime;

        /// <summary>Direction along a wall: velocity on the wall plane, or the model's forward when barely moving.</summary>
        public Vector3 WallForward(RaycastHit wallHit) => wallHit.WallForward(Rigidbody.linearVelocity, PlayerModel.forward);

        public Probe CreateGroundProbe(float distance, float radius) =>
            Probe.Sphere(Rigidbody, Vector3.down, distance, radius)
                .On(PlayerContext.GroundLayer)
                .Offset(Vector3.up * PlayerContext.GroundProbeHeight)
                .Gated(GroundGate);

        public Probe CreateRailProbe(float distance, float radius) =>
            Probe.Sphere(Rigidbody, Vector3.down, distance, radius)
                .On(PlayerContext.RailLayer)
                .Offset(Vector3.up * PlayerContext.GroundProbeHeight)
                .Gated(RailGate);

        /// <summary>Wall sphere cast. <paramref name="localDirection"/> is relative to the player model.</summary>
        public Probe CreateWallSphere(Vector3 localDirection, float distance, float radius) =>
            Probe.Sphere(Rigidbody, localDirection, distance, radius)
                .On(PlayerContext.WallLayer)
                .Offset(Vector3.up * PlayerContext.WallProbeHeight)
                .RelativeTo(PlayerModel)
                .Gated(WallGate);

        /// <summary>Wall raycast. <paramref name="localDirection"/> is relative to the player model.</summary>
        public Probe CreateWallRay(Vector3 localDirection, float distance) =>
            Probe.Ray(Rigidbody, localDirection, distance)
                .On(PlayerContext.WallLayer)
                .Offset(Vector3.up * PlayerContext.WallProbeHeight)
                .RelativeTo(PlayerModel)
                .Gated(WallGate);

        /// <summary>Bar overlap sphere. <paramref name="localDirection"/> is relative to the player model.</summary>
        public Probe CreateBarProbe(Vector3 localDirection, float distance, float radius) =>
            Probe.OverlapSphere(Rigidbody, localDirection, distance, radius)
                .On(PlayerContext.BarLayer)
                .Offset(Vector3.up * PlayerContext.BarProbeHeight)
                .RelativeTo(PlayerModel)
                .IncludeTriggers()
                .Gated(BarGate);

        #endregion

        #region Jump

        [Header("Jump")]
        [SerializeField] private int _maxJumps = 1;
        public int JumpsLeft => _maxJumps - _jumpsUsed;
        private int _jumpsUsed;
        public int JumpsUsed
        {
            get => _jumpsUsed;
            set => _jumpsUsed = Mathf.Clamp(value, 0, _maxJumps);
        }

        [SerializeField] private float _jumpToFallingTime;
        public float JumpToFallingTime
        {
            get => _jumpToFallingTime; set
            {
                _jumpToFallingTime = Mathf.Clamp(value, 0, _maxJumpToFallingTime);
            }
        }

        [SerializeField] private float _maxJumpToFallingTime = 1.2f;
        public float MaxJumpToFallingTime => _maxJumpToFallingTime;

        [SerializeField] private float _walkJumpToWalledTime;
        public float WalkJumpToWalledTime
        {
            get => _walkJumpToWalledTime; set
            {
                _walkJumpToWalledTime = Mathf.Clamp(value, 0, _maxWalkJumpToWalledTime);
            }
        }

        [SerializeField] private float _maxWalkJumpToWalledTime = 0.3f;
        public float MaxWalkJumpToWalledTime => _maxWalkJumpToWalledTime;

        [SerializeField] private float _jumpToWalledTime;
        public float JumpToWalledTime
        {
            get => _jumpToWalledTime;
            set
            {
                _jumpToWalledTime = Mathf.Clamp(value, 0, _maxJumpToWalledTime);
            }
        }

        [SerializeField] private float _maxJumpToWalledTime = 0.5f;
        public float MaxJumpToWalledTime => _maxJumpToWalledTime;

        public Vector3 JumpDirection { get; set; }

        [SerializeField] private float _jumpBufferTime = 0.15f;
        [SerializeField] private float _jumpReleaseTime = 0.1f;

        [SerializeField] private bool _airStrafeSpeedBoost;
        public bool AirStrafeSpeedBoost
        {
            get => _airStrafeSpeedBoost;
            set => _airStrafeSpeedBoost = value;
        }

        #endregion

        #region Move

        [Header("Move")]
        [SerializeField] private float _moveThreshold = 0.01f;

        [SerializeField] private bool _isMovementInput;
        public bool IsMovementInput => _isMovementInput;

        [SerializeField] private bool _isRunInput;
        public bool IsRunInput => _isRunInput;

        public Vector3 MoveDirection { get; set; }

        public Vector3 PlatformVelocity { get; set; }

        #endregion

        #region Step

        private float _stepUpGraceTime;
        public float StepUpGraceTime
        {
            get => _stepUpGraceTime;
            set => _stepUpGraceTime = Mathf.Max(value, 0f);
        }

        #endregion

        #region Stamina

        [Header("Stamina")]
        [SerializeField] private float _stamina;
        public float Stamina
        {
            get => _stamina;
            set
            {
                _stamina = Mathf.Clamp(value, 0, _maxStamina);
            }
        }

        [SerializeField]
        private float _maxStamina = 100f;
        public float MaxStamina => _maxStamina;

        [SerializeField] private Slider _staminaBar;

        #endregion

        private void Start()
        {
            Factory = new PlayerStateFactory(this);

            if (IsLocalPlayer)
            {
                CurrentState = Factory.GetState(PlayerStates.Airborne);
                CurrentState.EnterState();

                SwitchContextState(PlayerStates.ThirdPersonCamera);

                Stamina = MaxStamina;

                if (_staminaBar != null)
                    _staminaBar.maxValue = MaxStamina;
            }
        }

        private void Update()
        {
            if (IsLocalPlayer)
            {
                HandleInputs();

                UpdateActiveStates();

                if (_staminaBar != null)
                    _staminaBar.value = Stamina;
            }
        }

        private void FixedUpdate()
        {
            if (IsLocalPlayer)
            {
                if (_stepUpGraceTime > 0f)
                    _stepUpGraceTime -= Time.fixedDeltaTime;

                FixedUpdateActiveStates();

                CheckActiveStateSwitches();
            }
        }

        private void LateUpdate()
        {
            if (IsLocalPlayer)
            {
                LateUpdateActiveStates();
            }
        }

        // ─── Input ─── \\

        private void HandleInputs()
        {
            if (InputProvider == null)
            {
                Debug.LogError("InputProvider is not set in PlayerStateMachine.");
                return;
            }

            _isMovementInput = InputProvider.MovementState.RawInputValue.magnitude > _moveThreshold;
            _isRunInput = InputProvider.RunState.RawInputValue;

            HandleActiveStateInputs(InputProvider);
        }

        public bool IsLocalPlayer { get; set; } = true;

        public bool RotationSnapped { get; set; }

        // Add near other shared runtime detection/state fields

        public Vector3 BarPivot { get; set; }
        public Vector3 BarAxis { get; set; } = Vector3.right;
    }
}