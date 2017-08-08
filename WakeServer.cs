#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Wake.Protocol.Internal;
using Wake.Protocol.Proxy;
using Wake.Protocol.Proxy.Interfaces;
using MessageBase = Wake.Protocol.Proxy.Messages.MessageBase;

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
        public event Action<byte[], int> DataReceived;

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

        internal override void ProcessIncomingEvent(NetworkEventType netEvent, int connectionId, int channelId, byte[] buffer, int dataSize)
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
                    var packet = JsonUtility.FromJson<Packet>(Encoding.UTF8.GetString(buffer, 0, dataSize));
                    if (packet.Server)
                    {
                        if (string.IsNullOrEmpty(packet.ProxyId))
                        {
                            // handle raw packages
                            if (DataReceived != null) DataReceived(packet.Data, channelId);
                        }
                        else if (_proxys.ContainsKey(packet.ProxyId))
                        {
                            // pass data to proxy, it'll deserialize it to proper type
                            // and fires own event
                            _proxys[packet.ProxyId].ReceivedInternal(packet.Data, connectionId);
                        }
                        else
                        {
                            WakeNet.Log($"Unsupported or not registered proxy type : {packet.ProxyId}");
                        }
                    }
                    else
                    {
                        if (!_clients.ContainsKey(connectionId)) throw new Exception("Data received for disconnected client.");
                        
                        // pass data to client presentation on server side, it'll handle it by itself
                        _clients[connectionId].ProcessIncomingEvent(netEvent, connectionId, channelId, buffer, dataSize);
                    }
                    break;
            }
        }

        internal void ProcessOutgoingEvents()
        {
            if (_proxys == null) return;
            foreach (var k in _proxys.Keys)
            {
                if (_proxys[k].SendQueueCount <= 0) continue;
                Send(_proxys[k].PopMessageFromQueue(), _proxys[k].ChannelId, k, -1); // <---- TODO way to hanlde connection ids
            }

            foreach (var key in _clients.Keys)
                _clients[key].ProcessOutgoingEvents();
        }

        #endregion

        #region Explicit Interface Implementation

        private Dictionary<string, IProxy> _proxys;
        internal int ProxyCount => _proxys == null ? 0 : _proxys.Count;

        internal IProxy GetProxyAtIndex(int index)
        {
            if (_proxys == null) return null;
            if (_proxys.Count == 0) return null;
            return _proxys.Values.ElementAt(index);
        }
        
        public Proxy<TInMessage, TOutMessage> AddProxy<TInMessage, TOutMessage>(string proxyId, int channelId) where TInMessage : MessageBase where TOutMessage : MessageBase
        {
            if (_proxys == null) _proxys = new Dictionary<string, IProxy>();
            if (_proxys.ContainsKey(proxyId)) throw new Exception(string.Format("Proxy with ID - {0} already registered", proxyId));

            _proxys.Add(proxyId, new Proxy<TInMessage, TOutMessage>(channelId, true));
            return (Proxy<TInMessage, TOutMessage>)_proxys[proxyId];
        }

        public void RemoveProxy(string proxyId)
        {
            if (_proxys == null) return;
            if (!_proxys.ContainsKey(proxyId)) throw new Exception(string.Format("Proxy with ID - {0} not registered", proxyId));
            _proxys.Remove(proxyId);
        }

        public void Send(byte[] data, int channelId, string proxyId, int connectionId)
        {
            WakeNet.Log($"WakeServer:{Socket}:Send()");
            var packet = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Packet
            {
                Data = data,
                ProxyId = proxyId,
                Server = true
            }, true));
            byte error;
            if (connectionId >= 0)
                NetworkTransport.Send(Socket, connectionId, channelId, packet, packet.Length, out error);
            else
            {
                NetworkTransport.StartSendMulticast(Socket, channelId, data, data.Length, out error);

                foreach (var client in _clients.Keys)
                    NetworkTransport.SendMulticast(Socket, client, out error);

                NetworkTransport.FinishSendMulticast(Socket, out error);
            }
            if (error > 0) Error = error;
        }

        #endregion
    }
}