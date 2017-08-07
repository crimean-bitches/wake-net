namespace Wake.Protocol.Proxy.Messages
{
    public class StringMessage : MessageBase
    {
        public string Data;

        public StringMessage(string data)
        {
            Data = data;
        }
    }
}