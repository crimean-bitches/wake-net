#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helper;
using UnityEngine;
using UnityEngine.Networking;
using WakeNet.Internal;
using WakeNet.Protocol.Internal;
using WakeNet.Protocol.Proxy;
using WakeNet.Protocol.Proxy.Interfaces;
using MessageBase = WakeNet.Protocol.Proxy.Messages.MessageBase;

#endregion

namespace WakeNet
{
    public class Client : IProxyHandler
    {
        private NetClient _client;
        private int _hostId;

        public int ConnectionId { get { return _client.mConnection; } }
        public bool IsConnected { get { return _client.mIsConnected; } }

        public event Action Connected;
        public event Action Disconnected;
        public event Action<byte[], int> DataReceived;

        public Client() { }

        internal Client(int socket, int connection)
        {
            _client = new NetClient(socket, connection);
            _client.OnMessage = OnClientEvent;
        }

        public void Connect(string host, int port)
        {
            NetManager.Init();
            _client = NetManager.CreateClient();
            _client.Connect(host, port);
            _client.OnMessage = OnClientEvent;
            _client.OnSend = OnClientSend;
        }

        internal void OnClientSend()
        {
            if(_proxys == null) return;
            foreach (var k in _proxys.Keys)
            {
                if(_proxys[k].SendQueueCount <= 0) continue;
                Send(_proxys[k].PopMessageFromQueue(), _proxys[k].ChannelId, k);
            }
        }

        internal void OnClientEvent(NetworkEventType netEvent, int connectionid, int channelid, byte[] buffer, int datasize)
        {
            switch (netEvent)
            {
                case NetworkEventType.ConnectEvent:
                    if (Connected != null) Connected();
                    NetUtils.Log("Client| connected.", _hostId);
                    break;
                case NetworkEventType.DisconnectEvent:
                    if (Disconnected != null) Disconnected();
                    NetUtils.Log("Client| disconnected.", _hostId);
                    break;
                case NetworkEventType.DataEvent:
                    var packet = JsonUtility.FromJson<Packet>(Encoding.UTF8.GetString(buffer, 0, datasize));
                    if (packet.ProxyId == 0)
                    {
                        // handle raw packages
                        if (DataReceived != null) DataReceived(packet.Data, channelid);
                    }
                    else if (_proxys.ContainsKey(packet.ProxyId))
                    {
                        // pass data to proxy, it'll deserialize it to proper type
                        // and fires own event
                        _proxys[packet.ProxyId].ReceivedInternal(packet.Data);
                    }
                    else
                    {
                        NetUtils.Log("Unsupported or not registered proxy type : {0}", packet.ProxyId);
                    }
                    break;
            }
        }

        public void Disconnect()
        {
            _client.Disconnect();
        }

        public void Send(byte[] data, int channelId, ushort proxyId = 0)
        {
            var packet = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Packet
            {
                Data = data,
                ProxyId = proxyId
            }, true));
            _client.Send(packet, channelId);
        }

        #region Explicit Interface Implementation

        internal int ProxyCount { get { return _proxys == null ? 0 : _proxys.Count; } }
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
            if (_proxys.ContainsKey(proxyId)) throw new Exception(string.Format("Proxy with ID - {0} already registered", proxyId));
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