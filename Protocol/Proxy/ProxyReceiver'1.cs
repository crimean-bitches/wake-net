using System;
using Wake.Protocol.Proxy.Interfaces;

namespace Wake.Protocol.Proxy
{
    public sealed class ProxyReceiver<TMessage> : IProxyReceiver
    {
        public bool Server { get; private set; }
        public int ChannelId { get; private set; }

        public event Action<TMessage, int> Received; 

        public ProxyReceiver(int channelId, bool server)
        {
            ChannelId = channelId;
            Server = server;
        }

        public void ReceivedInternal(byte[] rawMessage, int connectionId)
        {
            throw new System.NotImplementedException();
        }
    }
}