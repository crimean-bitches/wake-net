namespace Wake.Protocol.Proxy.Messages
{
    public abstract class DataMessage<T> : MessageBase
    {
        public T Data;

        protected DataMessage(T data)
        {
            Data = data;
        }
    }
}