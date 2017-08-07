using Wake.Protocol.Proxy.Messages;

namespace Wake.Protocol.Proxy.Interfaces
{
    internal interface IProxy
    {
        int ChannelId { get; }
        int SendQueueCount { get; }

        byte[] PopMessageFromQueue();

        void ReceivedInternal(byte[] rawMessage);
        void SendInternal(MessageBase message);
    }
}