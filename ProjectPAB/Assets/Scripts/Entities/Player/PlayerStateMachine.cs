using Entities.Player.Detection;
using Entities.Player.States.Base;
using System;
using Systems.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Entities.Player
{
    [RequireComponent(typeof(PlayerContext))]
    public class PlayerStateMachine : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _doDebug = false;
        public bool DoDebug => _doDebug;

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

        #region StateTransitionEvents

        public event Action<PlayerStates> OnRootStateTransitioned;
        public event Action<PlayerStates> OnMovementStateTransitioned;
        public event Action<PlayerStates> OnActionStateTransitioned;
        public event Action<PlayerStates> OnContextStateTransitioned;

        public void InvokeRootStateTransitioned(PlayerStates state) => OnRootStateTransitioned?.Invoke(state);
        public void InvokeMovementStateTransitioned(PlayerStates state) => OnMovementStateTransitioned?.Invoke(state);
        public void InvokeActionStateTransitioned(PlayerStates state) => OnActionStateTransitioned?.Invoke(state);
        public void InvokeContextStateTransitioned(PlayerStates state) => OnContextStateTransitioned?.Invoke(state);

        #endregion

        private void Start()
        {
            _stateFactory = new PlayerStateFactory(this);

            if (IsLocalPlayer)
            {
                _currentRootState = _stateFactory.GetState(PlayerStates.Falling);
                _currentRootState.EnterState();

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

                _currentRootState?.UpdateStates();
                _currentContextState?.UpdateStates();

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

                GroundDetector.Tick();
                WallDetector.Tick(MoveDirection);
                RailDetector.Tick();

                _currentRootState?.FixedUpdateStates();
                _currentContextState?.FixedUpdateStates();

                _currentRootState?.CheckSwitchStates();
                _currentContextState?.CheckSwitchStates();
            }
        }

        private void LateUpdate()
        {
            if (IsLocalPlayer)
            {
                _currentRootState?.LateUpdateStates();
                _currentContextState?.LateUpdateStates();
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

            _currentRootState?.HandleInputActions(InputProvider);
            _currentContextState?.HandleInputActions(InputProvider);
        }

        public bool IsLocalPlayer { get; set; } = true;

        public bool RotationSnapped { get; set; }

        private bool _mStateToggle = false;

        // ─── State transitions ─── \\

        public void SwitchRootState(PlayerStates nextState)
        {
            PlayerBaseState previousState = _currentRootState;
            PlayerBaseState nextStateInstance = _stateFactory.GetState(nextState);

            _currentRootState?.ExitStates(nextStateInstance);

            _currentRootState = nextStateInstance;
            _currentRootState.EnterState(previousState);
        }

        public void SwitchMovementState(PlayerStates nextSubState)
        {
            if (_currentRootState == null) return;

            PlayerBaseState previousSub = _currentRootState.MovementSubState;
            PlayerBaseState nextInstance = _stateFactory.GetState(nextSubState);

            previousSub?.ExitStates(nextInstance);
            _currentRootState.MovementSubState = nextInstance;
            nextInstance?.EnterState(previousSub);
        }

        public void SwitchActionState(PlayerStates nextSubState)
        {
            if (_currentRootState == null) return;

            PlayerBaseState previousSub = _currentRootState.ActionSubState;
            PlayerBaseState nextInstance = _stateFactory.GetState(nextSubState);

            previousSub?.ExitStates(nextInstance);
            _currentRootState.ActionSubState = nextInstance;
            nextInstance?.EnterState(previousSub);
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

            InvokeContextStateTransitioned(contextState);
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