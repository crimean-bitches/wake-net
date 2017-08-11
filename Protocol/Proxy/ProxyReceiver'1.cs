using System.Text;
using UnityEngine;
using Wake.Protocol.Proxy.Interfaces;

namespace Wake.Protocol.Proxy
{
    public sealed class ProxyReceiver<TMessage> : IProxyReceiver
    {
        public bool Server { get; private set; }
        public int ChannelId { get; private set; }

        public event ProxyReceivedHandler<TMessage> Received; 

        public ProxyReceiver(int channelId, bool server)
        {
            ChannelId = channelId;
            Server = server;
        }

        public void ReceivedInternal(byte[] rawMessage, int connectionId)
        {
            var message = JsonUtility.FromJson<TMessage>(Encoding.UTF8.GetString(rawMessage));
            if (message == null) return;
            if(Received == null) return;
            Received(message, connectionId);
        }
    }
}