using System;
using System.Collections.Generic;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using SimpleTerrariaClone;
using static SimpleTerrariaClone.Map;

namespace Project1
{
    public class Server
    {
        private NetServer _netServer;
        private Dictionary<long, PlayerState> _playerStates = new Dictionary<long, PlayerState>();
        private int _nextPlayerId = 1;

        public const int Port = 14242;
        public const string AppIdentifier = "TerrariaClone";

        public void Start()
        {
            var config = new NetPeerConfiguration(AppIdentifier)
            {
                Port = Port,
                MaximumConnections = 2 // Limit to 2 players
            };
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            _netServer = new NetServer(config);
            _netServer.Start();
        }

        public void Stop()
        {
            _netServer.Shutdown("Server shutdown");
        }

        public void Update()
        {
            NetIncomingMessage msg;
            while ((msg = _netServer.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.ConnectionApproval:
                        HandleConnectionApproval(msg);
                        break;

                    case NetIncomingMessageType.Data:
                        HandleDataMessage(msg);
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        HandleStatusChange(msg);
                        break;
                }
                _netServer.Recycle(msg);
            }
        }

        private void HandleConnectionApproval(NetIncomingMessage msg)
        {
            if (_playerStates.Count >= 2)
            {
                msg.SenderConnection.Deny("Server full");
                return;
            }

            var playerId = _nextPlayerId++;
            var playerState = new PlayerState
            {
                NetworkId = playerId,
                Connection = msg.SenderConnection
            };

            _playerStates.Add(playerId, playerState);

            // Send approval with player ID
            var approval = _netServer.CreateMessage();
            approval.Write(playerId);
            msg.SenderConnection.Approve(approval);

            // Send existing players to new player
            SendInitialState(playerId);

            // Notify all players about new connection
            BroadcastPlayerJoined(playerId);
        }

        private void SendInitialState(int newPlayerId)
        {
            // Send existing players to the new player
            foreach (var existingPlayer in _playerStates.Values)
            {
                if (existingPlayer.NetworkId == newPlayerId)
                    continue;

                var msg = _netServer.CreateMessage();
                msg.Write((byte)NetworkMessageType.PlayerJoined);
                msg.Write(existingPlayer.NetworkId);
                _netServer.SendMessage(msg, _playerStates[newPlayerId].Connection, NetDeliveryMethod.ReliableOrdered);
            }
        }

        private void HandleDataMessage(NetIncomingMessage msg)
        {
            var messageType = (NetworkMessageType)msg.ReadByte();
            switch (messageType)
            {
                case NetworkMessageType.PlayerState:
                    UpdatePlayerState(msg);
                    break;

                case NetworkMessageType.TileChange:
                    HandleTileChange(msg);
                    break;
            }
        }

        private void HandleStatusChange(NetIncomingMessage msg)
        {
            var status = (NetConnectionStatus)msg.ReadByte();
            if (status == NetConnectionStatus.Disconnected)
            {
                // Find and remove disconnected player
                int playerId = -1;
                foreach (var kvp in _playerStates)
                {
                    if (kvp.Value.Connection == msg.SenderConnection)
                    {
                        playerId = (int)kvp.Key;
                        break;
                    }
                }

                if (playerId != -1)
                {
                    _playerStates.Remove(playerId);
                    BroadcastPlayerLeft(playerId);
                }
            }
        }

        private void UpdatePlayerState(NetIncomingMessage msg)
        {
            var playerId = msg.ReadInt32();
            if (!_playerStates.TryGetValue(playerId, out var state))
                return;

            state.Position = new Vector2(msg.ReadSingle(), msg.ReadSingle());
            state.AnimationFrame = msg.ReadInt32();

            BroadcastPlayerState(state);
        }

        private void HandleTileChange(NetIncomingMessage msg)
        {
            var change = new TileChange
            {
                X = msg.ReadInt32(),
                Y = msg.ReadInt32(),
                TileType = (Tiles)msg.ReadByte(),
                PlayerId = msg.ReadInt32()
            };

            if (ValidateTileChange(change))
            {
                BroadcastTileChange(change);
            }
        }

        private bool ValidateTileChange(TileChange change)
        {
            // Add proper validation logic here
            return true;
        }
        private void BroadcastPlayerState(PlayerState state)
        {
            var msg = _netServer.CreateMessage();
            msg.Write((byte)NetworkMessageType.PlayerState);
            msg.Write(state.NetworkId);
            msg.Write(state.Position.X);
            msg.Write(state.Position.Y);
            msg.Write(state.AnimationFrame);

            _netServer.SendToAll(msg, NetDeliveryMethod.UnreliableSequenced);
        }

        private void BroadcastTileChange(TileChange change)
        {
            var msg = _netServer.CreateMessage();
            msg.Write((byte)NetworkMessageType.TileChange);
            msg.Write(change.X);
            msg.Write(change.Y);
            msg.Write((byte)change.TileType);

            _netServer.SendToAll(msg, NetDeliveryMethod.ReliableOrdered);
        }

        private void BroadcastPlayerJoined(long playerId)
        {
            var msg = _netServer.CreateMessage();
            msg.Write((byte)NetworkMessageType.PlayerJoined);
            msg.Write((int)playerId);

            _netServer.SendToAll(msg, NetDeliveryMethod.ReliableOrdered);
        }

        private void BroadcastPlayerLeft(long playerId)
        {
            var msg = _netServer.CreateMessage();
            msg.Write((byte)NetworkMessageType.PlayerLeft);
            msg.Write((int)playerId);

            _netServer.SendToAll(msg, NetDeliveryMethod.ReliableOrdered);
        }

        private bool IsValidTileChange(TileChange change)
        {
            // Add validation logic here
            return true;
        }
    }
    public enum NetworkMessageType : byte
    {
        PlayerState,
        TileChange,
        PlayerJoined,
        PlayerLeft
    }

    public struct PlayerState
    {
        public int NetworkId;
        public Vector2 Position;
        public int AnimationFrame;
        public NetConnection Connection;
    }

    public struct TileChange
    {
        public int X;
        public int Y;
        public Tiles TileType;
        public int PlayerId;
    }
}