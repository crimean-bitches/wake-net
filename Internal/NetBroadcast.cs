using UnityEngine.Networking;

namespace WakeNet.Internal
{
    internal class NetBroadcast
    {
        public int mSocket = -1;

        public NetEventHandler OnMessage = null;

        private readonly int mPort;
        private readonly int mKey;
        private readonly int mVersion;
        private readonly int mSubversion;

        public bool mIsBroadcasting;
        public bool mIsListening;

        public NetBroadcast(int port, int key, int version, int subversion)
        {
            var csocket = NetworkTransport.AddHost(Config.GetHostTopology(128));

            if (!NetUtils.IsSocketValid(csocket)) NetUtils.LogWarning("NetBroadcast::CreateBroadcast() returned an invalid socket ( " + csocket + " )");

            mPort = port;
            mKey = key;
            mVersion = version;
            mSubversion = subversion;
            mSocket = csocket;
        }

        public void Broadcast(byte[] data, int timeout = 100)
        {
            byte error;
            NetworkTransport.StartBroadcastDiscovery(mSocket, mPort, mKey, mVersion, mSubversion, data, data.Length, 100, out error);
        }

        public void Listen()
        {
            mIsListening = true;
        }

        public bool Shutdown()
        {
            if (!mIsBroadcasting)
            {
                NetUtils.LogWarning("NetBroadcast::Shutdown() Failed with reason 'In not bradcasting for shutdown!");
                return false;
            }
            
            NetworkTransport.StopBroadcastDiscovery();

            mIsBroadcasting = false;
            mIsListening = false;
            return true;
        }
    }
}