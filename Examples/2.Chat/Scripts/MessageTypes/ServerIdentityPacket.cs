using Wake.Protocol.Messages;

namespace Assets.Plugins.Examples._2.Chat.Scripts.MessageTypes
{
    public class ServerIdentityPacket : DataMessage<int>
    {
        public ServerIdentityPacket(int data) : base(data)
        {
        }
    }
}