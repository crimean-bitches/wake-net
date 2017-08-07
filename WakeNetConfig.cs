#region Usings

using UnityEngine.Networking;

#endregion

namespace Wake
{
    public class WakeNetConfig
    {
        public static WakeNetConfig Default = new WakeNetConfig(new GlobalConfig(), new ConnectionConfig(),
            new[] {QosType.Reliable, QosType.Unreliable});

        public WakeNetConfig(GlobalConfig global, ConnectionConfig connection, QosType[] channels, int receiveRate = 50)
        {
            GlobalConfig = global;
            ConnectionConfig = connection;
            for (var i = 0; i < channels.Length; i++)
                ConnectionConfig.AddChannel(channels[i]);
            ReceiveRate = receiveRate;
        }

        public GlobalConfig GlobalConfig { get; }
        public ConnectionConfig ConnectionConfig { get; }
        public int ReceiveRate { get; }
    }
}