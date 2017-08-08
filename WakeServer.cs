#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

#endregion

namespace Wake
{
    public sealed class WakeServer : WakeObject
    {
        private readonly IDictionary<int, WakeClient> _clients;
        private readonly int _port;

        internal WakeServer(int maxConnections, int port, int simMinTimeout = 0, int simMaxTimeout = 0)
        {
            WakeNet.Log("WakeServer::Ctor()");
            _port = port;
            _clients = new Dictionary<int, WakeClient>();

            Socket = WakeNet.AddSocket(maxConnections, simMinTimeout, simMaxTimeout, _port);
            if (Socket >= 0)
                IsConnected = true;
        }

        public event Action<WakeClient> ClientConnected;
        public event Action<WakeClient> ClientDisconnected;

        public void DisconnectClient(int clientId)
        {
            WakeNet.Log($"WakeServer:{Socket}:DisconnectClient()");
            if (!_clients.ContainsKey(clientId))
            {
                WakeNet.Log(WakeError.ClientNotExists);
                return;
            }

            byte error;
            NetworkTransport.Disconnect(Socket, clientId, out error);
            _clients.Remove(clientId);
        }

        public void DisconnectAllClients()
        {
            WakeNet.Log($"WakeServer:{Socket}:DisconnectAllClients()");
            foreach (var clientId in _clients.Keys.ToList())
            {
                DisconnectClient(clientId);
            }
        }

        #region Internal

        internal override void ProcessIncomingEvent(NetworkEventType netEvent, int connectionId, int channelId,
            byte[] buffer, int dataSize)
        {
            switch (netEvent)
            {
                case NetworkEventType.ConnectEvent:
                    if (_clients.ContainsKey(connectionId))
                    {
                        WakeNet.Log(WakeError.ClientAlreadyExists);
                        return;
                    }

                    _clients.Add(connectionId, new WakeClient(Socket, connectionId));
                    if (ClientConnected != null) ClientConnected(_clients[connectionId]);
                    WakeNet.Log($"Server|{Socket}| connected [{0}].", connectionId);
                    break;
                case NetworkEventType.DisconnectEvent:
                    if (!_clients.ContainsKey(connectionId))
                    {
                        WakeNet.Log(WakeError.ClientNotExists);
                        return;
                    }

                    if (ClientDisconnected != null) ClientDisconnected(_clients[connectionId]);
                    _clients.Remove(connectionId);

                    WakeNet.Log($"Server|{Socket}| disconnected [{0}].", connectionId);
                    break;
                case NetworkEventType.DataEvent:
                    // TODO reorganize this thing to allow server receive direct messages, not only user avatar
                    if (!_clients.ContainsKey(connectionId))
                        throw new Exception("Data received for disconnected client.");
                    _clients[connectionId].ProcessIncomingEvent(netEvent, connectionId, channelId, buffer, dataSize);
                    break;
            }
        }

        internal void ProcessOutgoingEvents()
        {
            foreach (var key in _clients.Keys)
                _clients[key].ProcessOutgoingEvents();
        }

        #endregion
    }
}