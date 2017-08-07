using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Wake.Protocol.Proxy.Interfaces;
using Wake.Protocol.Proxy.Messages;

namespace Wake.Protocol.Proxy
{
    public sealed class Proxy<TInMessage, TOutMessage> : IProxy where TInMessage : MessageBase where TOutMessage : MessageBase
    {
        private int _channelId;
        private Queue<byte[]> _sendQueue;

        public int ChannelId { get { return _channelId; } }
        public int SendQueueCount { get { return _sendQueue.Count; } }

        public event Action<TInMessage> Received;

        internal Proxy(int channelId)
        {
            _channelId = channelId;
            _sendQueue = new Queue<byte[]>();
        }

        public void Send(TOutMessage message)
        {
            SendInternal(message);
        }

        #region Implicit Interface Implementation
        
        public int ConnectionId { get; set; }

        public byte[] PopMessageFromQueue()
        {
            return _sendQueue.Dequeue();
        }

        public void ReceivedInternal(byte[] rawMessage)
        {
            var message = JsonUtility.FromJson<TInMessage>(Encoding.UTF8.GetString(rawMessage));
            if (Received != null) Received(message);
        }

        public void SendInternal(MessageBase message)
        {
            _sendQueue.Enqueue(Encoding.UTF8.GetBytes(JsonUtility.ToJson(message)));
        }

        #endregion
    }
}