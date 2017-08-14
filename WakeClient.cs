#region Usings

using System;
using System.Collections.Generic;
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
    public sealed class WakeClient : WakeObject
    {
        private readonly Dictionary<string, IProxySender> _proxySenders;
        private readonly Dictionary<string, IProxyReceiver> _proxyReceivers;

        public int Port { get; private set; }
        public string Host { get; private set; }
        public int ConnectionId { get; private set; }
        
        public event Action Connected;
        public event Action Disconnected;
        public event Action<byte[], int> DataReceived;

        internal WakeClient()
        {
            WakeNet.Log("WakeClient::Ctor()");

            _proxySenders = new Dictionary<string, IProxySender>();
            _proxyReceivers = new Dictionary<string, IProxyReceiver>();

            // require single connection
            Socket = WakeNet.AddSocket(1);
        }

        internal WakeClient(int socket, int connectionId)
        {
            WakeNet.Log("WakeClient::Ctor()");

            _proxySenders = new Dictionary<string, IProxySender>();
            _proxyReceivers = new Dictionary<string, IProxyReceiver>();

            Socket = socket;
            ConnectionId = connectionId;
            IsConnected = true;
        }

        public void Connect(string host, int port)
        {
            WakeNet.Log(string.Format("WakeClient:{0}:Connect()", Socket));
            byte error;
            ConnectionId = NetworkTransport.Connect(Socket, host, port, 0, out error);
            if (error > 0)
            {
                Error = error;
            }
            else
            {
                IsConnected = true;
                Host = host;
                Port = port;
            }
        }

        public void Disconnect()
        {
            WakeNet.Log(string.Format("WakeClient:{0}:Disconnect()", Socket));
            if (!IsConnected)
            {
                WakeNet.Log(WakeError.NotConnected);
                return;
            }

            byte error;
            NetworkTransport.Disconnect(Socket, ConnectionId, out error);
            if (error > 0) Error = error;
        }

        public void Send(byte[] data, int channelId, string proxyId, bool server)
        {
            var packet = WakeNet.Serialize(new Packet
            {
                Data = data,
                ProxyId = proxyId,
                Server = server
            });
            WakeNet.Log(string.Format("WakeClient:{0}:Send() - {1}b", Socket, packet.Length));
            byte error;
            NetworkTransport.Send(Socket, ConnectionId, channelId, packet, packet.Length, out error);
            if (error > 0) Error = error;
        }

        internal override void ProcessIncomingEvent(NetworkEventType netEvent, int connectionId, int channelId, byte[] buffer, int dataSize)
        {
            switch (netEvent)
            {
                case NetworkEventType.ConnectEvent:
                    if (Connected != null) Connected();
                    WakeNet.Log(string.Format("Client[{0}] - connected.", ConnectionId), NetworkLogLevel.Informational);
                    break;
                case NetworkEventType.DisconnectEvent:
                    if (Disconnected != null) Disconnected();
                    WakeNet.Log(string.Format("Client[{0}] - disconnected.", ConnectionId), NetworkLogLevel.Informational);
                    break;
                case NetworkEventType.DataEvent:
                    WakeNet.Log("Client[{0}] - Packet : {1}b", NetworkLogLevel.Full, ConnectionId, dataSize);
                    var packet = WakeNet.Deserialzie<Packet>(buffer, 0, dataSize);
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
                        WakeNet.Log(string.Format("Unsupported or not registered proxy type : {0}", packet.ProxyId), NetworkLogLevel.Informational);
                    }
                    break;
            }
        }

        internal void ProcessOutgoingEvents()
        {
            foreach (var k in _proxySenders.Keys)
            {
                if (_proxySenders[k].SendQueueCount <= 0) continue;
                var m = _proxySenders[k].PopMessageFromQueue();
                Send(m, _proxySenders[k].ChannelId, k, _proxySenders[k].Server);
                WakeNet.Log("Proxy[{0}] (Client) - Send : {1}b", NetworkLogLevel.Full, k, m.Length);
            }
        }

        public ProxySender<TMessage> AddProxySender<TMessage>(string proxyId, int channelId, bool server) where TMessage : MessageBase
        {
            if (_proxySenders.ContainsKey(proxyId)) throw new Exception(
                string.Format("Proxy sender with ID - {0} already registered", proxyId));
            _proxySenders.Add(proxyId, new ProxySender<TMessage>(channelId, server));
            return (ProxySender<TMessage>)_proxySenders[proxyId];
        }

        public void RemoveProxySender(string proxyId)
        {
            if (!_proxySenders.ContainsKey(proxyId)) throw new Exception(
                string.Format("Proxy sender with ID - {0} not registered", proxyId));
            _proxySenders.Remove(proxyId);
        }

        public ProxyReceiver<TMessage> AddProxyReceiver<TMessage>(string proxyId, int channelId, bool server) where TMessage : MessageBase
        {
            if (_proxyReceivers.ContainsKey(proxyId)) throw new Exception(
                string.Format("Proxy receiver with ID - {0} already registered", proxyId));
            _proxyReceivers.Add(proxyId, new ProxyReceiver<TMessage>(channelId, server));
            return (ProxyReceiver<TMessage>)_proxyReceivers[proxyId];
        }

        public void RemoveProxyReceiver(string proxyId)
        {
            if (!_proxyReceivers.ContainsKey(proxyId)) throw new Exception(
                string.Format("Proxy receiver with ID - {0} not registered", proxyId));
            _proxyReceivers.Remove(proxyId);
        }

        public Proxy<TInMessage, TOutMessage> AddProxy<TInMessage, TOutMessage>(string proxyId, int channelId, bool server)
            where TInMessage : MessageBase where TOutMessage : MessageBase
        {
            if (_proxySenders.ContainsKey(proxyId)) throw new Exception(string.Format("Proxy sender with ID - {0} already registered", proxyId));
            if (_proxyReceivers.ContainsKey(proxyId)) throw new Exception(string.Format("Proxy receiver with ID - {0} already registered", proxyId));

            var proxy = new Proxy<TInMessage, TOutMessage>(channelId, server);
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