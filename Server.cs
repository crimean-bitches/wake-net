#region Usings

using System;
using System.Collections.Generic;
using Helper;
using UnityEngine.Networking;
using WakeNet.Internal;

#endregion

namespace WakeNet
{
    public class Server
    {
        private readonly int _port;
        private NetServer _server;

        private byte error;

        public Server(int port)
        {
            _port = port;
        }

        public event Action<Client> ClientConnected;
        public event Action<Client> ClientDisconnected;

        private IDictionary<int, Client> _clients;

        public void Start()
        {
            NetManager.Init();
            _server = NetManager.CreateServer(2, _port);
            _server.OnMessage = OnServerMessage;
            _server.OnSend = OnServerSend;
            _clients = new Dictionary<int, Client>();
        }

        private void OnServerSend()
        {
            foreach (var key in _clients.Keys)
                _clients[key].OnClientSend();
        }

        private void OnServerMessage(NetworkEventType netEvent, int connectionId, int channeld, byte[] buffer, int datasize)
        {
            switch (netEvent)
            {
                case NetworkEventType.ConnectEvent:
                    if(_clients.ContainsKey(connectionId)) throw new Exception("Client with same ID connected already.");
                    
                    _clients.Add(connectionId, new Client(_server.mSocket, connectionId));
                    if (ClientConnected != null) ClientConnected(_clients[connectionId]);

                    FLog.LogFormat("Server| connected [{0}].", connectionId);
                    break;
                case NetworkEventType.DisconnectEvent:
                    if (!_clients.ContainsKey(connectionId)) throw new Exception("Client with ID disconnected already.");

                    if (ClientDisconnected != null) ClientDisconnected(_clients[connectionId]);
                    _clients.Remove(connectionId);

                    FLog.LogFormat("Server| disconnected [{0}].", connectionId);
                    break;
                case NetworkEventType.DataEvent:
                    if (!_clients.ContainsKey(connectionId)) throw new Exception("Data received for disconnected client.");
                    _clients[connectionId].OnClientEvent(netEvent, connectionId, channeld, buffer, datasize);
                    break;
            }
        }

        public void Stop()
        {
            if (_server != null) NetManager.DestroyServer(_server);
        }
        
        #region Explicit Interface Implementation

        #endregion
    }
}