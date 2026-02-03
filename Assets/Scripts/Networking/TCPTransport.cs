using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace PlunkAndPlunder.Networking
{
    /// <summary>
    /// Direct TCP connection transport for LAN/Internet play
    /// Host runs a server, clients connect via IP:Port
    /// </summary>
    public class TCPTransport : INetworkTransport
    {
        public bool IsHost { get; private set; }
        public bool IsConnected { get; private set; }
        public string LocalPlayerId { get; private set; }

        public event Action<string> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;
        public event Action<NetworkMessage> OnMessageReceived;
        public event Action<string> OnConnectionError;

        private const int DEFAULT_PORT = 7777;
        private const int BUFFER_SIZE = 8192;

        // Server state (host only)
        private TcpListener _listener;
        private Dictionary<string, ClientConnection> _clients = new Dictionary<string, ClientConnection>();
        private Thread _acceptThread;
        private bool _isRunning;

        // Client state (client only)
        private TcpClient _client;
        private NetworkStream _clientStream;
        private Thread _clientReceiveThread;

        // Player tracking
        private Dictionary<string, NetworkPlayerInfo> _players = new Dictionary<string, NetworkPlayerInfo>();
        private int _nextClientId = 1;
        private string _localPlayerName = "Player";

        // Main thread event queue (Unity isn't thread-safe)
        private ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

        public void Initialize()
        {
            LocalPlayerId = Guid.NewGuid().ToString();
            Debug.Log($"[TCPTransport] Initialized with ID: {LocalPlayerId}");
        }

        /// <summary>
        /// Set the local player's display name
        /// </summary>
        public void SetLocalPlayerName(string name)
        {
            _localPlayerName = string.IsNullOrWhiteSpace(name) ? "Player" : name;

            // Update in player list
            if (_players.ContainsKey(LocalPlayerId))
            {
                _players[LocalPlayerId] = new NetworkPlayerInfo(LocalPlayerId, _localPlayerName, _players[LocalPlayerId].isReady);
            }

            // Broadcast update if connected
            if (IsConnected)
            {
                if (IsHost)
                {
                    BroadcastPlayerList();
                }
                else
                {
                    // Send name update to host
                    SendRawMessage(_clientStream, $"NAME:{_localPlayerName}");
                }
            }

            Debug.Log($"[TCPTransport] Local player name set to: {_localPlayerName}");
        }

        public void CreateLobby(int maxPlayers)
        {
            if (IsConnected)
            {
                EnqueueMainThreadAction(() => OnConnectionError?.Invoke("Already connected"));
                return;
            }

            try
            {
                IsHost = true;
                _isRunning = true;

                // Start listening on all network interfaces
                _listener = new TcpListener(IPAddress.Any, DEFAULT_PORT);
                _listener.Start();

                // Add host as first player
                _players[LocalPlayerId] = new NetworkPlayerInfo(LocalPlayerId, _localPlayerName, false);

                // Start accepting connections on background thread
                _acceptThread = new Thread(AcceptClientsLoop);
                _acceptThread.IsBackground = true;
                _acceptThread.Start();

                IsConnected = true;

                string localIP = GetLocalIPAddress();
                Debug.Log($"[TCPTransport] Host started on {localIP}:{DEFAULT_PORT}");
                Debug.Log($"[TCPTransport] Share this IP with friends: {localIP}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TCPTransport] Failed to create lobby: {ex.Message}");
                EnqueueMainThreadAction(() => OnConnectionError?.Invoke($"Failed to host: {ex.Message}"));
            }
        }

        public void JoinLobby(string lobbyId)
        {
            if (IsConnected)
            {
                EnqueueMainThreadAction(() => OnConnectionError?.Invoke("Already connected"));
                return;
            }

            // lobbyId is "IP:Port" or just "IP" (defaults to 7777)
            string[] parts = lobbyId.Split(':');
            string ip = parts[0];
            int port = parts.Length > 1 ? int.Parse(parts[1]) : DEFAULT_PORT;

            try
            {
                IsHost = false;
                _client = new TcpClient();
                _client.Connect(ip, port);
                _clientStream = _client.GetStream();

                // Send join message with our player ID and name
                SendRawMessage(_clientStream, $"JOIN:{LocalPlayerId}:{_localPlayerName}");

                // Start receiving messages
                _isRunning = true;
                _clientReceiveThread = new Thread(() => ClientReceiveLoop(_clientStream));
                _clientReceiveThread.IsBackground = true;
                _clientReceiveThread.Start();

                IsConnected = true;
                Debug.Log($"[TCPTransport] Connected to {ip}:{port}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TCPTransport] Failed to join: {ex.Message}");
                EnqueueMainThreadAction(() => OnConnectionError?.Invoke($"Failed to connect: {ex.Message}"));
            }
        }

        public void LeaveLobby()
        {
            _isRunning = false;
            IsConnected = false;

            // Close client connection
            if (!IsHost)
            {
                try
                {
                    _clientStream?.Close();
                    _client?.Close();
                }
                catch { }
            }

            // Close server
            if (IsHost)
            {
                try
                {
                    _listener?.Stop();

                    foreach (var client in _clients.Values)
                    {
                        client.stream?.Close();
                        client.tcpClient?.Close();
                    }

                    _clients.Clear();
                }
                catch { }
            }

            _players.Clear();
            Debug.Log("[TCPTransport] Left lobby");
        }

        public void SendMessage(NetworkMessage message, bool reliable = true)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[TCPTransport] Cannot send message - not connected");
                return;
            }

            string json = JsonUtility.ToJson(message);

            if (IsHost)
            {
                // Broadcast to all clients
                foreach (var client in _clients.Values.ToList())
                {
                    try
                    {
                        SendRawMessage(client.stream, $"MSG:{json}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[TCPTransport] Failed to send to client {client.playerId}: {ex.Message}");
                    }
                }
            }
            else
            {
                // Send to host
                try
                {
                    SendRawMessage(_clientStream, $"MSG:{json}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TCPTransport] Failed to send to host: {ex.Message}");
                    EnqueueMainThreadAction(() => OnConnectionError?.Invoke("Connection lost"));
                }
            }
        }

        public void SendMessageToPlayer(string playerId, NetworkMessage message, bool reliable = true)
        {
            if (!IsHost)
            {
                Debug.LogWarning("[TCPTransport] Only host can send to specific players");
                return;
            }

            if (_clients.TryGetValue(playerId, out var client))
            {
                string json = JsonUtility.ToJson(message);
                try
                {
                    SendRawMessage(client.stream, $"MSG:{json}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TCPTransport] Failed to send to {playerId}: {ex.Message}");
                }
            }
        }

        public List<NetworkPlayerInfo> GetConnectedPlayers()
        {
            return _players.Values.ToList();
        }

        public void Shutdown()
        {
            LeaveLobby();
        }

        // Called from Unity's Update loop
        public void ProcessMainThreadQueue()
        {
            while (_mainThreadActions.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TCPTransport] Error processing main thread action: {ex.Message}");
                }
            }
        }

        #region Server (Host) Logic

        private void AcceptClientsLoop()
        {
            Debug.Log("[TCPTransport] Accepting clients...");

            while (_isRunning)
            {
                try
                {
                    if (_listener.Pending())
                    {
                        TcpClient tcpClient = _listener.AcceptTcpClient();
                        NetworkStream stream = tcpClient.GetStream();

                        // Start handling this client on a new thread
                        Thread clientThread = new Thread(() => HandleClient(tcpClient, stream));
                        clientThread.IsBackground = true;
                        clientThread.Start();
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Debug.LogError($"[TCPTransport] Accept error: {ex.Message}");
                    }
                }
            }
        }

        private void HandleClient(TcpClient tcpClient, NetworkStream stream)
        {
            string playerId = null;

            try
            {
                // Read join message
                string joinMsg = ReadRawMessage(stream);
                if (!joinMsg.StartsWith("JOIN:"))
                {
                    Debug.LogWarning("[TCPTransport] Client didn't send JOIN message");
                    tcpClient.Close();
                    return;
                }

                // Parse JOIN:playerId:playerName
                string[] joinParts = joinMsg.Substring(5).Split(':');
                playerId = joinParts[0];
                string playerName = joinParts.Length > 1 ? joinParts[1] : $"Player{_nextClientId++}";

                Debug.Log($"[TCPTransport] Client connected: {playerId} as {playerName}");

                // Store client
                var clientConnection = new ClientConnection
                {
                    tcpClient = tcpClient,
                    stream = stream,
                    playerId = playerId
                };
                _clients[playerId] = clientConnection;

                // Add to players list
                _players[playerId] = new NetworkPlayerInfo(playerId, playerName, false);

                // Notify on main thread
                EnqueueMainThreadAction(() => OnPlayerJoined?.Invoke(playerId));

                // Send current player list to new client
                BroadcastPlayerList();

                // Receive messages from this client
                while (_isRunning && tcpClient.Connected)
                {
                    string rawMsg = ReadRawMessage(stream);
                    if (rawMsg == null) break;

                    if (rawMsg.StartsWith("MSG:"))
                    {
                        string json = rawMsg.Substring(4);
                        NetworkMessage msg = JsonUtility.FromJson<NetworkMessage>(json);

                        // Relay to all other clients
                        foreach (var otherClient in _clients.Values.Where(c => c.playerId != playerId))
                        {
                            try
                            {
                                SendRawMessage(otherClient.stream, rawMsg);
                            }
                            catch { }
                        }

                        // Notify host
                        EnqueueMainThreadAction(() => OnMessageReceived?.Invoke(msg));
                    }
                    else if (rawMsg.StartsWith("NAME:"))
                    {
                        // Handle name update
                        string newName = rawMsg.Substring(5);
                        if (_players.ContainsKey(playerId))
                        {
                            _players[playerId] = new NetworkPlayerInfo(playerId, newName, _players[playerId].isReady);
                            Debug.Log($"[TCPTransport] Player {playerId} changed name to: {newName}");
                            BroadcastPlayerList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TCPTransport] Client {playerId} disconnected: {ex.Message}");
            }
            finally
            {
                // Cleanup
                if (playerId != null)
                {
                    _clients.Remove(playerId);
                    _players.Remove(playerId);
                    EnqueueMainThreadAction(() => OnPlayerLeft?.Invoke(playerId));
                }

                tcpClient?.Close();
            }
        }

        private void BroadcastPlayerList()
        {
            // Send current players to all clients
            var playerListMsg = new NetworkMessage(
                NetworkMessageType.GameStateUpdate,
                LocalPlayerId,
                "PLAYER_LIST:" + string.Join(",", _players.Keys)
            );

            SendMessage(playerListMsg);
        }

        #endregion

        #region Client Logic

        private void ClientReceiveLoop(NetworkStream stream)
        {
            while (_isRunning)
            {
                try
                {
                    string rawMsg = ReadRawMessage(stream);
                    if (rawMsg == null) break;

                    if (rawMsg.StartsWith("MSG:"))
                    {
                        string json = rawMsg.Substring(4);
                        NetworkMessage msg = JsonUtility.FromJson<NetworkMessage>(json);

                        // Handle player list updates
                        if (msg.type == NetworkMessageType.GameStateUpdate && msg.payload.StartsWith("PLAYER_LIST:"))
                        {
                            string playerIds = msg.payload.Substring(12);
                            UpdatePlayerList(playerIds.Split(','));
                        }

                        EnqueueMainThreadAction(() => OnMessageReceived?.Invoke(msg));
                    }
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Debug.LogError($"[TCPTransport] Receive error: {ex.Message}");
                        EnqueueMainThreadAction(() => OnConnectionError?.Invoke("Connection lost"));
                    }
                    break;
                }
            }
        }

        private void UpdatePlayerList(string[] playerIds)
        {
            _players.Clear();
            int playerNum = 1;
            foreach (string id in playerIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    _players[id] = new NetworkPlayerInfo(id, $"Player{playerNum++}", false);
                }
            }
        }

        #endregion

        #region Helpers

        private void SendRawMessage(NetworkStream stream, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

            stream.Write(lengthPrefix, 0, 4);
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        private string ReadRawMessage(NetworkStream stream)
        {
            // Read length prefix (4 bytes)
            byte[] lengthBuffer = new byte[4];
            int bytesRead = 0;
            while (bytesRead < 4)
            {
                int read = stream.Read(lengthBuffer, bytesRead, 4 - bytesRead);
                if (read == 0) return null; // Connection closed
                bytesRead += read;
            }

            int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
            if (messageLength <= 0 || messageLength > BUFFER_SIZE)
            {
                throw new Exception($"Invalid message length: {messageLength}");
            }

            // Read message data
            byte[] messageBuffer = new byte[messageLength];
            bytesRead = 0;
            while (bytesRead < messageLength)
            {
                int read = stream.Read(messageBuffer, bytesRead, messageLength - bytesRead);
                if (read == 0) return null; // Connection closed
                bytesRead += read;
            }

            return Encoding.UTF8.GetString(messageBuffer);
        }

        private void EnqueueMainThreadAction(Action action)
        {
            _mainThreadActions.Enqueue(action);
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }

            return "127.0.0.1";
        }

        #endregion

        #region Helper Classes

        private class ClientConnection
        {
            public TcpClient tcpClient;
            public NetworkStream stream;
            public string playerId;
        }

        #endregion
    }
}
