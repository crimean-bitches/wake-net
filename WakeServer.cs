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
using MessageBase = Wake.Protocol.Messages.MessageBase;

#endregion

namespace Wake
{
    public sealed class WakeServer : WakeObject
    {
        private readonly IDictionary<int, WakeClient> _clients;

        private readonly Dictionary<string, IProxySender> _proxySenders;
        private readonly Dictionary<string, IProxyReceiver> _proxyReceivers;

        private readonly int _port;
        
        public int ClientCount { get { return _clients.Count; } }
        public int Port { get { return _port; } }

        public event Action<WakeClient> ClientConnected;
        public event Action<WakeClient> ClientDisconnected;
        public event Action<byte[], int> DataReceived;

        internal WakeServer(int maxConnections, int port, int simMinTimeout = 0, int simMaxTimeout = 0)
        {
            WakeNet.Log("WakeServer::Ctor()");
            _port = port;
            _clients = new Dictionary<int, WakeClient>();

            _proxySenders = new Dictionary<string, IProxySender>();
            _proxyReceivers = new Dictionary<string, IProxyReceiver>();

            Socket = WakeNet.AddSocket(maxConnections, simMinTimeout, simMaxTimeout, _port);
            if (Socket >= 0)
                IsConnected = true;
        }

        public void DisconnectClient(int clientId)
        {
            WakeNet.Log(string.Format("WakeServer:{0}:DisconnectClient()", Socket));
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
            WakeNet.Log(string.Format("WakeServer:{0}:DisconnectAllClients()", Socket));
            foreach (var clientId in _clients.Keys.ToList())
            {
                DisconnectClient(clientId);
            }
        }

        public void Send(byte[] data, int channelId, string proxyId, int connectionId)
        {
            var packet = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Packet
            {
                Data = data,
                ProxyId = proxyId,
                Server = true
            }, true));
            WakeNet.Log(string.Format("WakeServer:{0}:Send()\n", Socket) + Encoding.UTF8.GetString(packet));
            byte error = 0;
            if (connectionId >= 0)
                NetworkTransport.Send(Socket, connectionId, channelId, packet, packet.Length, out error);
            else
                foreach (var client in _clients.Keys)
                    NetworkTransport.Send(Socket, client, channelId, packet, packet.Length, out error);
            if (error > 0) Error = error;
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
                    WakeNet.Log(string.Format("Server|{0}| connected [{1}].", Socket, connectionId), NetworkLogLevel.Informational);
                    break;
                case NetworkEventType.DisconnectEvent:
                    if (!_clients.ContainsKey(connectionId))
                    {
                        WakeNet.Log(WakeError.ClientNotExists);
                        return;
                    }

                    if (ClientDisconnected != null) ClientDisconnected(_clients[connectionId]);
                    _clients.Remove(connectionId);

                    WakeNet.Log(string.Format("Server|{0}| disconnected [{1}].", Socket, connectionId), NetworkLogLevel.Informational);
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
                        else if (_proxyReceivers.ContainsKey(packet.ProxyId))
                        {
                            // pass data to proxy, it'll deserialize it to proper type
                            // and fires own event
                            _proxyReceivers[packet.ProxyId].ReceivedInternal(packet.Data, connectionId);
                        }
                        else
                        {
                            WakeNet.Log(string.Format("Unsupported or not registered proxy type : {0}", packet.ProxyId));
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
            if (_proxySenders == null) return;
            foreach (var k in _proxySenders.Keys)
            {
                if (_proxySenders[k].SendQueueCount <= 0) continue;
                Send(_proxySenders[k].PopMessageFromQueue(), _proxySenders[k].ChannelId, k, -1); // <---- TODO way to hanlde connection ids
            }

            foreach (var key in _clients.Keys)
                _clients[key].ProcessOutgoingEvents();
        }

        #endregion

        public ProxySender<TMessage> AddProxySender<TMessage>(string proxyId, int channelId) where TMessage : MessageBase
        {
            if (_proxySenders.ContainsKey(proxyId)) throw new Exception(
                string.Format("Proxy sender with ID - {0} already registered", proxyId));
            _proxySenders.Add(proxyId, new ProxySender<TMessage>(channelId, true));
            return (ProxySender<TMessage>)_proxySenders[proxyId];
        }

        public void RemoveProxySender(string proxyId)
        {
            if (!_proxySenders.ContainsKey(proxyId)) throw new Exception(
                string.Format("Proxy sender with ID - {0} not registered", proxyId));
            _proxySenders.Remove(proxyId);
        }

        public ProxyReceiver<TMessage> AddProxyReceiver<TMessage>(string proxyId, int channelId) where TMessage : MessageBase
        {
            if (_proxyReceivers.ContainsKey(proxyId)) throw new Exception(
                string.Format("Proxy receiver with ID - {0} already registered", proxyId));
            _proxyReceivers.Add(proxyId, new ProxyReceiver<TMessage>(channelId, true));
            return (ProxyReceiver<TMessage>)_proxyReceivers[proxyId];
        }

        public void RemoveProxyReceiver(string proxyId)
        {
            if (!_proxyReceivers.ContainsKey(proxyId)) throw new Exception(
                string.Format("Proxy receiver with ID - {0} not registered", proxyId));
            _proxyReceivers.Remove(proxyId);
        }

        public Proxy<TInMessage, TOutMessage> AddProxy<TInMessage, TOutMessage>(string proxyId, int channelId)
            where TInMessage : MessageBase where TOutMessage : MessageBase
        {
            if (_proxySenders.ContainsKey(proxyId)) throw new Exception(string.Format("Proxy sender with ID - {0} already registered", proxyId));
            if (_proxyReceivers.ContainsKey(proxyId)) throw new Exception(string.Format("Proxy receiver with ID - {0} already registered", proxyId));

            var proxy = new Proxy<TInMessage, TOutMessage>(channelId, true);
            _proxySenders.Add(proxyId, proxy);
            _proxyReceivers.Add(proxyId, proxy);
            return proxy;
        }

        public void RemoveProxy(string proxyId)
        {
            if (!_proxySenders.ContainsKey(proxyId)) throw new Exception(
                string.Format("Proxy sender with ID - {0} not registered", proxyId));
            if (!_proxyReceivers.ContainsKey(proxyId)) throw new Exception(
                string.Format("Proxy receiver with ID - {0} not registered", proxyId));
            _proxySenders.Remove(proxyId);
            _proxyReceivers.Remove(proxyId);
        }
    }
}