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
    public sealed class WakeClient : WakeObject, IProxyHandler
    {
        internal WakeClient()
        {
            WakeNet.Log("WakeClient::Ctor()");
            // require single connection
            Socket = WakeNet.AddSocket(1);
        }

        internal WakeClient(int socket, int connectionId)
        {
            WakeNet.Log("WakeClient::Ctor()");
            Socket = socket;
            ConnectionId = connectionId;
            IsConnected = true;
        }

        public int Port { get; private set; }
        public string Host { get; private set; }
        public int ConnectionId { get; private set; }
        
        public event Action Connected;
        public event Action Disconnected;
        public event Action<byte[], int> DataReceived;

        public void Send(byte[] data, int channelId, ushort proxyId = 0)
        {
            WakeNet.Log("WakeClient::Send()");
            var packet = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Packet
            {
                Data = data,
                ProxyId = proxyId
            }, true));
            byte error;
            NetworkTransport.Send(Socket, ConnectionId, channelId, packet, packet.Length, out error);
            if (error > 0) Error = error;
        }

        public void Connect(string host, int port)
        {
            WakeNet.Log("WakeClient::Connect()");
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
            WakeNet.Log("WakeClient::Disconnect()");
            if (!IsConnected)
            {
                WakeNet.Log(WakeError.NotConnected);
                return;
            }

            byte error;
            NetworkTransport.Disconnect(Socket, ConnectionId, out error);
            if (error > 0) Error = error;
        }

        internal override void ProcessIncomingEvent(NetworkEventType netEvent, int connectionId, int channelId,
            byte[] buffer, int dataSize)
        {
            switch (netEvent)
            {
                case NetworkEventType.ConnectEvent:
                    if (Connected != null) Connected();
                    WakeNet.Log("Client| connected.");
                    break;
                case NetworkEventType.DisconnectEvent:
                    if (Disconnected != null) Disconnected();
                    WakeNet.Log("Client| disconnected.");
                    break;
                case NetworkEventType.DataEvent:
                    var packet = JsonUtility.FromJson<Packet>(Encoding.UTF8.GetString(buffer, 0, dataSize));
                    if (packet.ProxyId == 0)
                    {
                        // handle raw packages
                        if (DataReceived != null) DataReceived(packet.Data, channelId);
                    }
                    else if (_proxys.ContainsKey(packet.ProxyId))
                    {
                        // pass data to proxy, it'll deserialize it to proper type
                        // and fires own event
                        _proxys[packet.ProxyId].ReceivedInternal(packet.Data);
                    }
                    else
                    {
                        WakeNet.Log("Unsupported or not registered proxy type : {0}", packet.ProxyId);
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
                Send(_proxys[k].PopMessageFromQueue(), _proxys[k].ChannelId, k);
            }
        }

        #region Explicit Interface Implementation

        internal int ProxyCount => _proxys == null ? 0 : _proxys.Count;

        internal IProxy GetProxyAtIndex(int index)
        {
            if (_proxys == null) return null;
            if (_proxys.Count == 0) return null;
            return _proxys.Values.ElementAt(index);
        }

        private Dictionary<ushort, IProxy> _proxys;

        public Proxy<TInMessage, TOutMessage> AddProxy<TInMessage, TOutMessage>(ushort proxyId, int channelId)
            where TInMessage : MessageBase where TOutMessage : MessageBase
        {
            if (_proxys == null) _proxys = new Dictionary<ushort, IProxy>();
            if (_proxys.ContainsKey(proxyId))
                throw new Exception(string.Format("Proxy with ID - {0} already registered", proxyId));
            _proxys.Add(proxyId, new Proxy<TInMessage, TOutMessage>(channelId));
            return (Proxy<TInMessage, TOutMessage>) _proxys[proxyId];
        }

        public void RemoveProxy(ushort proxyId)
        {
            if (_proxys == null) _proxys = new Dictionary<ushort, IProxy>();
            if (!_proxys.ContainsKey(proxyId))
                throw new Exception(string.Format("Proxy with ID - {0} not registered", proxyId));
            _proxys.Remove(proxyId);
        }

        #endregion
    }
}