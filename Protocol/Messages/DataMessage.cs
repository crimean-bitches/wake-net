namespace Wake.Protocol.Messages
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