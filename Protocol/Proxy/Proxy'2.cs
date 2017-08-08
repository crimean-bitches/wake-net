#region Usings

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Wake.Protocol.Proxy.Interfaces;
using Wake.Protocol.Proxy.Messages;

#endregion

namespace Wake.Protocol.Proxy
{
    public sealed class Proxy<TInMessage, TOutMessage> : IProxy where TInMessage : MessageBase where TOutMessage : MessageBase
    {
        private readonly Queue<byte[]> _sendQueue;
        private readonly bool _server;

        public int ChannelId { get; private set; }
        public bool Server { get { return _server; } }
        public int SendQueueCount { get { return _sendQueue.Count; } }

        public event Action<TInMessage, int> Received;

        internal Proxy(int channelId, bool server)
        {
            ChannelId = channelId;
            _sendQueue = new Queue<byte[]>();
            _server = server;
        }

        public void Send(TOutMessage message)
        {
            SendInternal(message);
        }

        #region IProxy
        
        public byte[] PopMessageFromQueue()
        {
            return _sendQueue.Dequeue();
        }

        public void ReceivedInternal(byte[] rawMessage, int connectionId)
        {
            var message = JsonUtility.FromJson<TInMessage>(Encoding.UTF8.GetString(rawMessage));
            if (Received != null) Received(message, connectionId);
        }

        public void SendInternal(MessageBase message)
        {
            _sendQueue.Enqueue(Encoding.UTF8.GetBytes(JsonUtility.ToJson(message)));
        }

        #endregion
    }
}