using System;

namespace Wake.Protocol.Messages
{
    [Serializable]
    public abstract class DataMessage<T> : MessageBase
    {
        public T Data;

        protected DataMessage(T data)
        {
            Data = data;
        }
    }
}