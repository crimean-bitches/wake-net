#region Usings

using System;
using Wake.Protocol.Messages;
using Wake.Protocol.Proxy.Interfaces;

#endregion

namespace Wake.Protocol.Proxy
{
    public sealed class Proxy<TInMessage, TOutMessage> : IProxy where TInMessage : MessageBase where TOutMessage : MessageBase
    {
        public bool Server { get; private set; }
        public int ChannelId { get; private set; }

        private readonly ProxySender<TOutMessage> _sender;
        private readonly ProxyReceiver<TInMessage> _receiver;

        public int SendQueueCount
        {
            get { return _sender.SendQueueCount; }
        }

        public event ProxyReceivedHandler<TInMessage> Received; 

        public Proxy(int channelId, bool server)
        {
            _sender = new ProxySender<TOutMessage>(channelId, server);
            _receiver = new ProxyReceiver<TInMessage>(channelId, server);

            ChannelId = channelId;
            Server = server;

            _receiver.Received += ReceivedInvokation;
        }

        public void ReceivedInternal(byte[] rawMessage, int connectionId)
        {
            _receiver.ReceivedInternal(rawMessage, connectionId);
        }

        public byte[] PopMessageFromQueue()
        {
            return _sender.PopMessageFromQueue();
        }

        public void Send(TOutMessage message)
        {
            _sender.Send(message);
        }

        void IProxySender.Send(MessageBase message)
        {
            _sender.Send(message);
        }
        
        private void ReceivedInvokation(TInMessage message, int connectionId)
        {
            if (Received != null) Received(message, connectionId);
        }
    }
}