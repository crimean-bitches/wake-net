namespace Wake.Protocol.Proxy.Interfaces
{
    internal interface IProxyReceiver
    {
        bool Server { get; }
        int ChannelId { get; }
        
        void ReceivedInternal(byte[] rawMessage, int connectionId);
    }
}