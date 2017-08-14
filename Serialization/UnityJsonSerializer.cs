using System.Text;
using UnityEngine;

namespace Wake.Serialization
{
    public class UnityJsonSerializer : ISerializer
    {
        public byte[] Serialize(object data)
        {
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
        }

        public T Deserialize<T>(byte[] data, int offset, int length)
        {
            return JsonUtility.FromJson<T>(Encoding.UTF8.GetString(data, 0, length));
        }
    }
}