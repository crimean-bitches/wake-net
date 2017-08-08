#region Usings

using Wake.Protocol.Proxy.Messages;

#endregion

namespace Wake.Protocol.Proxy.Interfaces
{
    internal interface IProxy
    {
        int ChannelId { get; }
        int SendQueueCount { get; }
        bool Server { get; }

        byte[] PopMessageFromQueue();

        void ReceivedInternal(byte[] rawMessage, int connectionId);
        void SendInternal(MessageBase message);
    }
}