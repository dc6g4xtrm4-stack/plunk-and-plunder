using System;
using System.Collections.Generic;
using PlunkAndPlunder.Players;

namespace PlunkAndPlunder.Networking
{
    /// <summary>
    /// Interface for network transport layer (offline, Steam, etc.)
    /// </summary>
    public interface INetworkTransport
    {
        bool IsHost { get; }
        bool IsConnected { get; }
        string LocalPlayerId { get; }

        event Action<string> OnPlayerJoined;
        event Action<string> OnPlayerLeft;
        event Action<NetworkMessage> OnMessageReceived;
        event Action<string> OnConnectionError;

        void Initialize();
        void CreateLobby(int maxPlayers);
        void JoinLobby(string lobbyId);
        void LeaveLobby();
        void SendMessage(NetworkMessage message, bool reliable = true);
        void SendMessageToPlayer(string playerId, NetworkMessage message, bool reliable = true);
        List<NetworkPlayerInfo> GetConnectedPlayers();
        void Shutdown();
    }

    [Serializable]
    public class NetworkMessage
    {
        public NetworkMessageType type;
        public string senderId;
        public string payload;

        public NetworkMessage(NetworkMessageType type, string senderId, string payload = "")
        {
            this.type = type;
            this.senderId = senderId;
            this.payload = payload;
        }
    }

    public enum NetworkMessageType
    {
        PlayerReady,
        OrdersSubmitted,
        TurnResolved,
        GameStateUpdate,
        ChatMessage
    }

    [Serializable]
    public class NetworkPlayerInfo
    {
        public string playerId;
        public string playerName;
        public bool isReady;

        public NetworkPlayerInfo(string playerId, string playerName, bool isReady = false)
        {
            this.playerId = playerId;
            this.playerName = playerName;
            this.isReady = isReady;
        }
    }
}
