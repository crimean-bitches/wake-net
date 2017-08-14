#region Usings

using UnityEngine;
using UnityEngine.Networking;
using Wake.Serialization;

#endregion

namespace Wake
{
    public class WakeNetConfig
    {
        public static WakeNetConfig Default = new WakeNetConfig(
            new GlobalConfig(), 
            new ConnectionConfig(),
            new[] {QosType.Reliable, QosType.Unreliable, QosType.Unreliable },
            new UnityJsonSerializer());

        public WakeNetConfig(GlobalConfig global, ConnectionConfig connection, QosType[] channels, ISerializer serializer, int receiveRate = 50, NetworkLogLevel logLevel = NetworkLogLevel.Informational)
        {
            GlobalConfig = global;
            ConnectionConfig = connection;
            Serialzier = serializer;
            LogLevel = logLevel;
            for (var i = 0; i < channels.Length; i++)
                ConnectionConfig.AddChannel(channels[i]);
            ReceiveRate = receiveRate;
        }

        public GlobalConfig GlobalConfig { get; private set; }
        public ISerializer Serialzier { get; private set; }
        public ConnectionConfig ConnectionConfig { get; private set; }
        public int ReceiveRate { get; private set; }
        public NetworkLogLevel LogLevel { get; private set; }
    }
}