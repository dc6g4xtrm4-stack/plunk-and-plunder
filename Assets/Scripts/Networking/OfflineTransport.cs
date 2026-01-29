using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlunkAndPlunder.Networking
{
    /// <summary>
    /// Offline/local transport for single-player and hotseat
    /// </summary>
    public class OfflineTransport : INetworkTransport
    {
        public bool IsHost => true;
        public bool IsConnected => isInitialized;
        public string LocalPlayerId => "local_player_0";

        public event Action<string> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;
        public event Action<NetworkMessage> OnMessageReceived;
        public event Action<string> OnConnectionError;

        private bool isInitialized = false;
        private List<NetworkPlayerInfo> players = new List<NetworkPlayerInfo>();

        public void Initialize()
        {
            isInitialized = true;
            players.Clear();
            players.Add(new NetworkPlayerInfo(LocalPlayerId, "Local Player", false));
            Debug.Log("[OfflineTransport] Initialized");
        }

        public void CreateLobby(int maxPlayers)
        {
            Debug.Log($"[OfflineTransport] Creating offline lobby for {maxPlayers} players");
            // Nothing to do for offline
        }

        public void JoinLobby(string lobbyId)
        {
            Debug.LogWarning("[OfflineTransport] Cannot join lobby in offline mode");
        }

        public void LeaveLobby()
        {
            Debug.Log("[OfflineTransport] Leaving offline lobby");
        }

        public void SendMessage(NetworkMessage message, bool reliable = true)
        {
            // In offline mode, messages are processed locally
            // This is mainly for testing the message flow
            Debug.Log($"[OfflineTransport] Message sent: {message.type}");
        }

        public void SendMessageToPlayer(string playerId, NetworkMessage message, bool reliable = true)
        {
            Debug.Log($"[OfflineTransport] Message sent to {playerId}: {message.type}");
        }

        public List<NetworkPlayerInfo> GetConnectedPlayers()
        {
            return new List<NetworkPlayerInfo>(players);
        }

        public void Shutdown()
        {
            isInitialized = false;
            players.Clear();
            Debug.Log("[OfflineTransport] Shutdown");
        }
    }
}
