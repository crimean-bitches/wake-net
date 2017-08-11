using Wake.Protocol.Messages;

namespace Wake.Protocol.Proxy.Interfaces
{
    internal interface IProxySender
    {
        bool Server { get; }
        int ChannelId { get; }

        int SendQueueCount { get; }
        byte[] PopMessageFromQueue();
        void Send(MessageBase message);
    }
}