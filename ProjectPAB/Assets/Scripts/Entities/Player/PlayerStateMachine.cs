using Entities.Player.Detection;
using Entities.Player.States.Base;
using Systems.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Entities.Player
{
    [RequireComponent(typeof(PlayerContext))]
    public class PlayerStateMachine : MonoBehaviour
    {
        [Header("State System")]
        [SerializeField] private PlayerStateFactory _stateFactory;
        public PlayerStateFactory Factory => _stateFactory;

        [SerializeField] private PlayerBaseState _currentRootState;
        public PlayerBaseState CurrentState
        {
            get => _currentRootState; set => _currentRootState = value;
        }

        private PlayerBaseState _currentContextState;
        public PlayerBaseState CurrentContextState => _currentContextState;

        [SerializeField] private PlayerContext _playerContext;
        public PlayerContext PlayerContext
        {
            get => _playerContext; private set { _playerContext = value; }
        }

        public IInputProvider InputProvider => PlayerContext.InputProvider;
        public Transform PlayerObject => PlayerContext.PlayerModel;
        public Transform Orientation => PlayerContext.Orientation;
        public Transform Transform => PlayerContext.transform;
        public Rigidbody Rigidbody => PlayerContext.Rigidbody;
        public Collider Collider => PlayerContext.Collider;
        public Animator Animator => PlayerContext.Animator;

        // Camera proxies
        public Transform CameraHolder => PlayerContext.CameraHolder;
        public Transform FirstPersonAnchor => PlayerContext.FirstPersonAnchor;

        // Detection proxies
        public GroundDetector GroundDetector => PlayerContext.GroundDetector;
        public WallDetector WallDetector => PlayerContext.WallDetector;
        public RailDetector RailDetector => PlayerContext.RailDetector;

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

        private bool _jumpBuffered;
        private float _lastJumpPressedTime;
        private bool _jumpHeld;
        private bool _jumpLock;

        #endregion

        #region Move

        [Header("Move")]
        [SerializeField] private float _moveThreshold = 0.01f;

        [SerializeField] private bool _isMovementInput;
        public bool IsMovementInput => _isMovementInput;

        [SerializeField] private bool _isRunInput;
        public bool IsRunInput => _isRunInput;

        public Vector3 MoveDirection { get; set; }

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
            _playerContext = GetComponent<PlayerContext>();

            _stateFactory = new PlayerStateFactory(this);
            _currentRootState = _stateFactory.GetState(PlayerStates.Falling);
            _currentRootState.EnterState();

            SwitchContextState(PlayerStates.ThirdPersonCamera);

            Stamina = MaxStamina;

            if (_staminaBar != null)
                _staminaBar.maxValue = MaxStamina;
        }

        private void Update()
        {
            HandleInputs();

            _currentRootState?.UpdateStates();
            _currentContextState?.UpdateStates();

            if (_staminaBar != null)
                _staminaBar.value = Stamina;
        }

        private void FixedUpdate()
        {
            if (_stepUpGraceTime > 0f)
                _stepUpGraceTime -= Time.fixedDeltaTime;

            GroundDetector.Tick();
            WallDetector.Tick();
            RailDetector.Tick();

            _currentRootState?.FixedUpdateStates();
            _currentContextState?.FixedUpdateStates();

            _currentRootState?.CheckSwitchStates();
            _currentContextState?.CheckSwitchStates();
        }

        private void LateUpdate()
        {
            _currentRootState?.LateUpdateStates();
            _currentContextState?.LateUpdateStates();
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

            _currentRootState.HandleMoveInputs(InputProvider.MovementState);
            _currentRootState.HandleRunInputs(InputProvider.RunState);
            _currentRootState.HandleShiftInputs(InputProvider.ShiftState);
            _currentRootState.HandleSlideInputs(InputProvider.SlideState);
            _currentRootState.HandleJumpInputs(InputProvider.JumpState);

            _currentContextState?.HandleMoveInputs(InputProvider.MovementState);
            _currentContextState?.HandleRunInputs(InputProvider.RunState);
            _currentContextState?.HandleShiftInputs(InputProvider.ShiftState);
            _currentContextState?.HandleSlideInputs(InputProvider.SlideState);
        }

        private bool _mStateToggle = false;

        // ─── State transitions ─── \\

        public void TransitionTo(PlayerStates nextState)
        {
            PlayerBaseState previousState = _currentRootState;
            PlayerBaseState nextStateInstance = _stateFactory.GetState(nextState);

            _currentRootState?.ExitStates(nextStateInstance);

            _currentRootState = nextStateInstance;
            _currentRootState.EnterState(previousState);
        }

        /// <summary>
        /// Switches the active context state. Context lives at the machine level
        /// and persists across root state transitions automatically.
        /// </summary>
        public void SwitchContextState(PlayerStates contextState)
        {
            PlayerBaseState newContext = _stateFactory.GetState(contextState);
            if (newContext == null || newContext.StateType != PlayerStateType.Context) return;
            if (newContext.StateKey == _currentContextState?.StateKey) return;

            PlayerBaseState previousContext = _currentContextState;
            _currentContextState?.ExitStates(newContext);

            _currentContextState = newContext;
            _currentContextState.EnterState(previousContext);
            _currentContextState.InitializeSubStates();
        }

        public void ClearContextState()
        {
            _currentContextState?.ExitStates(null);
            _currentContextState = null;
        }
    }
}