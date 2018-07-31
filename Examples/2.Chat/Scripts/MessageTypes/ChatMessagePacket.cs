using Wake.Protocol.Messages;

public class ChatMessagePacket : DataMessage<string>
{
    public string Sender;

    public ChatMessagePacket(string sender, string data) : base(data)
    {
        Sender = sender;
    }
}
