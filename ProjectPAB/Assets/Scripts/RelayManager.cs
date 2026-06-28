using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RelayManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _startGameButton;
    [SerializeField] private TMP_InputField _joinCodeInput;
    [SerializeField] private TMP_Text _joinCodeDisplay;
    [SerializeField] private TMP_Text _statusText;

    [Header("Settings")]
    [SerializeField] private int _maxPlayers = 4;
    [SerializeField] private string _gameSceneName = "Multiplayer";

    private void Start()
    {
        _hostButton.onClick.AddListener(() => _ = OnHostClicked());
        _joinButton.onClick.AddListener(() => _ = OnJoinClicked());
        _startGameButton.onClick.AddListener(OnStartGameClicked);
        _startGameButton.gameObject.SetActive(false);
    }

    // ─── UI Handlers ─── \\

    private async Task OnHostClicked()
    {
        SetStatus("Creating host...");
        _hostButton.interactable = false;
        _joinButton.interactable = false;

        string joinCode = await StartHostWithRelay(_maxPlayers);

        if (joinCode != null)
        {
            _joinCodeDisplay.text = $"Join Code: {joinCode}";
            SetStatus("Share the code, then press Start when ready.");
            _startGameButton.gameObject.SetActive(true);
        }
        else
        {
            SetStatus("Failed to start host.");
            _hostButton.interactable = true;
            _joinButton.interactable = true;
        }
    }

    private async Task OnJoinClicked()
    {
        string code = _joinCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            SetStatus("Enter a join code first.");
            return;
        }

        SetStatus("Joining...");
        _hostButton.interactable = false;
        _joinButton.interactable = false;
        _joinCodeInput.interactable = false;

        await StartClientWithRelay(code);
    }

    private void OnStartGameClicked()
    {
        _startGameButton.interactable = false;
        NetworkManager.Singleton.SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
            _statusText.text = message;
    }

    // ─── Relay ─── \\

    private async Task<string> StartHostWithRelay(int maxPlayers)
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return NetworkManager.Singleton.StartHost() ? joinCode : null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Host failed: {e.Message}");
            return null;
        }
    }

    private async Task StartClientWithRelay(string joinCode)
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            NetworkManager.Singleton.StartClient();
            SetStatus("Waiting for host to start...");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Join failed: {e.Message}");
            SetStatus("Failed to join. Check the code and try again.");
            _hostButton.interactable = true;
            _joinButton.interactable = true;
            _joinCodeInput.interactable = true;
        }
    }
}