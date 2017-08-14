namespace Wake.Serialization
{
    public interface ISerializer
    {
        byte[] Serialize(object data);
        T Deserialize<T>(byte[] data, int offset, int length);
    }
}