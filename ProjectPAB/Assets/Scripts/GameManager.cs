using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private string _gameSceneName = "Multiplayer";

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayerForClient;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayerForClient;
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName != _gameSceneName) return;

        foreach (ulong clientId in clientsCompleted)
            SpawnPlayer(clientId);
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        if (SceneManager.GetActiveScene().name != _gameSceneName) return;
        SpawnPlayer(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {
        NetworkObject player = Instantiate(_playerPrefab);
        player.SpawnAsPlayerObject(clientId);
    }
}
