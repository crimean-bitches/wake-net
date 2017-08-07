using Wake.Protocol.Proxy.Messages;

namespace Wake.Protocol.Proxy.Interfaces
{
    public interface IProxyHandler
    {
        int ConnectionId { get; }

        Proxy<TInMessage, TOutMessage> AddProxy<TInMessage, TOutMessage>(ushort proxyId, int channelId) where TInMessage : MessageBase where TOutMessage : MessageBase;
        void RemoveProxy(ushort proxyId);
        void Send(byte[] data, int channelId, ushort proxyId = 0);
    }
}