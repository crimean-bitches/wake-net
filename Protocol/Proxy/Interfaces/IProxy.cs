#region Usings

using Wake.Protocol.Proxy.Messages;

#endregion

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