using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Wake.Protocol.Messages;
using Wake.Protocol.Proxy.Interfaces;

namespace Wake.Protocol.Proxy
{
    public sealed class ProxySender<TMessage> : IProxySender where TMessage : MessageBase
    {
        private readonly Queue<byte[]> _queue;

        public bool Server { get; private set; }
        public int ChannelId { get; private set; }

        public ProxySender(int channelId, bool server)
        {
            _queue = new Queue<byte[]>();
            ChannelId = channelId;
            Server = server;
        }

        public int SendQueueCount
        {
            get { return _queue.Count; }
        }

        public byte[] PopMessageFromQueue()
        {
            return _queue.Dequeue();
        }

        public void Send(MessageBase message)
        {
            _queue.Enqueue(WakeNet.Serialize(message));
        }
    }
}