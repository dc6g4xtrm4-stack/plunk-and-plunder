using System;
using UnityEngine;

namespace PlunkAndPlunder.Networking
{
    /// <summary>
    /// Manages network transport and coordinates multiplayer flow
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        public INetworkTransport Transport { get; private set; }
        public NetworkMode Mode { get; private set; }

        public event Action OnLobbyCreated;
        public event Action OnLobbyJoined;
        public event Action<string> OnError;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Default to offline
            SetMode(NetworkMode.Offline);
        }

        public void SetMode(NetworkMode mode)
        {
            Mode = mode;

            // Cleanup old transport
            Transport?.Shutdown();

            // Create new transport
            switch (mode)
            {
                case NetworkMode.Offline:
                    Transport = new OfflineTransport();
                    break;

                case NetworkMode.Steam:
                    Transport = new SteamTransport();
                    break;

                case NetworkMode.DirectConnection:
                    Transport = new TCPTransport();
                    break;
            }

            Transport.Initialize();
            SubscribeToTransportEvents();

            Debug.Log($"[NetworkManager] Mode set to {mode}");
        }

        private void SubscribeToTransportEvents()
        {
            Transport.OnPlayerJoined += HandlePlayerJoined;
            Transport.OnPlayerLeft += HandlePlayerLeft;
            Transport.OnMessageReceived += HandleMessageReceived;
            Transport.OnConnectionError += HandleConnectionError;
        }

        public void CreateLobby(int maxPlayers)
        {
            Transport.CreateLobby(maxPlayers);
            OnLobbyCreated?.Invoke();
        }

        public void JoinLobby(string lobbyId)
        {
            Transport.JoinLobby(lobbyId);
        }

        public void LeaveLobby()
        {
            Transport.LeaveLobby();
        }

        private void HandlePlayerJoined(string playerId)
        {
            Debug.Log($"[NetworkManager] Player joined: {playerId}");
        }

        private void HandlePlayerLeft(string playerId)
        {
            Debug.Log($"[NetworkManager] Player left: {playerId}");
        }

        private void HandleMessageReceived(NetworkMessage message)
        {
            Debug.Log($"[NetworkManager] Message received: {message.type} from {message.senderId}");
            // Forward to GameManager or other systems as needed
        }

        private void HandleConnectionError(string error)
        {
            Debug.LogError($"[NetworkManager] Connection error: {error}");
            OnError?.Invoke(error);
        }

        private void Update()
        {
            // Process events from background threads (needed for TCP transport)
            if (Transport is TCPTransport tcpTransport)
            {
                tcpTransport.ProcessMainThreadQueue();
            }
        }

        private void OnDestroy()
        {
            Transport?.Shutdown();
        }
    }

    public enum NetworkMode
    {
        Offline,
        Steam,
        DirectConnection
    }
}
