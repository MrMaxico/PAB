using Entities.Player;
using System;
using Unity.Netcode;
using UnityEngine;

namespace Systems.Input
{
    public class PlayerNetworkController : NetworkBehaviour
    {
        [SerializeField] private LocalInputProvider _localInputProvider;
        [SerializeField] private NetworkInputProvider _networkInputProvider;
        [SerializeField] private PlayerContext _playerContext;

        [SerializeField] private Camera _camera;
        [SerializeField] private AudioListener _audioListener;
        [SerializeField] private PlayerStateMachine _stateMachine;

        private NetworkVariable<PlayerStates> _networkedRootState = new(
            PlayerStates.Falling,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private NetworkVariable<PlayerStates> _networkedMovementState = new(
            PlayerStates.Idling,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private NetworkVariable<PlayerStates> _networkedActionState = new(
            PlayerStates.PrimaryAction,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private NetworkVariable<PlayerStates> _networkedContextState = new(
            PlayerStates.ThirdPersonCamera,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private Action<PlayerStates> _onRootTransitioned;
        private Action<PlayerStates> _onMovementTransitioned;
        private Action<PlayerStates> _onActionTransitioned;
        private Action<PlayerStates> _onContextTransitioned;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _playerContext.SetInputProvider(_localInputProvider);
                _stateMachine.IsLocalPlayer = true;
                _camera.enabled = true;
                _audioListener.enabled = true;
                _localInputProvider.enabled = true;
                _networkInputProvider.enabled = false;

                _stateMachine.OnRootStateTransitioned += _onRootTransitioned = s => _networkedRootState.Value = s;
                _stateMachine.OnMovementStateTransitioned += _onMovementTransitioned = s => _networkedMovementState.Value = s;
                _stateMachine.OnActionStateTransitioned += _onActionTransitioned = s => _networkedActionState.Value = s;
                _stateMachine.OnContextStateTransitioned += _onContextTransitioned = s => _networkedContextState.Value = s;
            }
            else
            {
                _playerContext.SetInputProvider(_networkInputProvider);
                _stateMachine.IsLocalPlayer = false;
                _camera.enabled = false;
                _audioListener.enabled = false;
                _localInputProvider.enabled = false;
                _networkInputProvider.enabled = true;

                _networkedRootState.OnValueChanged += OnRootStateChanged;
                _networkedMovementState.OnValueChanged += OnMovementStateChanged;
                _networkedActionState.OnValueChanged += OnActionStateChanged;
                _networkedContextState.OnValueChanged += OnContextStateChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _stateMachine.OnRootStateTransitioned -= _onRootTransitioned;
                _stateMachine.OnMovementStateTransitioned -= _onMovementTransitioned;
                _stateMachine.OnActionStateTransitioned -= _onActionTransitioned;
                _stateMachine.OnContextStateTransitioned -= _onContextTransitioned;
            }
            else
            {
                _networkedRootState.OnValueChanged -= OnRootStateChanged;
                _networkedMovementState.OnValueChanged -= OnMovementStateChanged;
                _networkedActionState.OnValueChanged -= OnActionStateChanged;
                _networkedContextState.OnValueChanged -= OnContextStateChanged;
            }
        }

        private void OnRootStateChanged(PlayerStates previousState, PlayerStates nextState)
        {
            _stateMachine.SwitchRootState(nextState);
        }

        private void OnMovementStateChanged(PlayerStates previousState, PlayerStates nextState)
        {
            _stateMachine.SwitchMovementState(nextState);
        }

        private void OnActionStateChanged(PlayerStates previousState, PlayerStates nextState)
        {
            _stateMachine.SwitchActionState(nextState);
        }

        private void OnContextStateChanged(PlayerStates previousState, PlayerStates nextState)
        {
            _stateMachine.SwitchContextState(nextState);
        }

        private int _lastSentTick;

        private void Update()
        {
            if (IsOwner)
            {
                if (NetworkManager.Singleton.LocalTime.Tick != _lastSentTick)
                {
                    _lastSentTick = NetworkManager.Singleton.LocalTime.Tick;
                    SendInputServerRpc(GatherLocalInput());
                    _stateMachine.RotationSnapped = false;
                }
            }
            else
            {
                ApplyModelRotation(
                    _networkInputProvider.LastModelRotation,
                    _networkInputProvider.LastSnapRotation
                );
            }
        }

        private void ApplyModelRotation(Quaternion target, bool snap)
        {
            if (snap)
                _playerContext.PlayerModel.rotation = target;
            else
                _playerContext.PlayerModel.rotation = Quaternion.Slerp(
                    _playerContext.PlayerModel.rotation, target, Time.deltaTime * 20f);
        }

        // Owner -> Server
        [ServerRpc]
        private void SendInputServerRpc(NetworkedInputs networkedInput)
        {
            if (!IsOwner)
                _networkInputProvider.UpdateNetworkInputs(networkedInput);

            ApplyInputClientRpc(networkedInput);
        }

        // Server -> all clients
        [ClientRpc]
        private void ApplyInputClientRpc(NetworkedInputs networkedInput)
        {
            if (!IsOwner)
                _networkInputProvider.UpdateNetworkInputs(networkedInput);
        }

        private NetworkedInputs GatherLocalInput() => new()
        {
            Move = _localInputProvider.MoveState.RawInputValue,
            Mouse = _localInputProvider.MouseState.RawInputValue,
            Buttons = _localInputProvider.GetButtonFlags(),
            ModelRotation = _playerContext.PlayerModel.rotation,
            SnapRotation = _stateMachine.RotationSnapped,
        };
    }
}
