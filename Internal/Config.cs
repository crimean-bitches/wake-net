#region Usings

using UnityEngine.Networking;

#endregion

namespace WakeNet.Internal
{
    internal class Config
    {
        public const int THREAD_POOL_SIZE = 8;
        public const int RECEIVE_RATE = 50;
        public const int PACKET_SIZE = 4096;
        public const int FRAGMENT_SIZE = PACKET_SIZE / 2;

        public static HostTopology GetHostTopology(int maxConnections)
        {
            return new HostTopology(GetConnectionConfig(), maxConnections);
        }

        public static ConnectionConfig GetConnectionConfig()
        {
            var cc = new ConnectionConfig();
            cc.AddChannel(QosType.Reliable);
            cc.AddChannel(QosType.Unreliable);
            //cc.PacketSize = PACKET_SIZE;
            //cc.FragmentSize = FRAGMENT_SIZE;
            return cc;
        }

        public static GlobalConfig GetClobalConfig()
        {
            var gc = new GlobalConfig();
            gc.ReactorModel = ReactorModel.FixRateReactor;
            //gc.MaxPacketSize = PACKET_SIZE;
            gc.ThreadPoolSize = THREAD_POOL_SIZE;
            return gc;
        }
    }
}