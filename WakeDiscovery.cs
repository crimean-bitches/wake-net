using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Wake
{
    public sealed class WakeDiscovery : WakeObject
    {
        private int _port;
        private int _key;
        private int _version;
        private int _subversion;

        public bool IsBroadcasting { get; private set; }
        public bool IsSearching { get; private set; }

        public WakeDiscovery(int port, int key, int version, int subversion)
        {
            WakeNet.Log("WakeDiscovery::Ctor()");

            Socket = WakeNet.AddSocket(32);

            _port = port;
            _key = key;
            _version = version;
            _subversion = subversion;
        }

        public void Broadcast(string broadcastMessage)
        {
            WakeNet.Log("WakeDiscovery::Broadcast()");
            if(IsBroadcasting) return;

            var data = Encoding.UTF8.GetBytes(SystemInfo.deviceUniqueIdentifier.Substring(0, 16) + broadcastMessage);
            byte error;
            NetworkTransport.StartBroadcastDiscovery(Socket, _port, _key, _version, _subversion, data, data.Length, 50, out error);
            if (error > 0) Error = error;
            else IsBroadcasting = true;
        }

        public void Search()
        {
            WakeNet.Log("WakeDiscovery::Search()");
            byte error;
            NetworkTransport.SetBroadcastCredentials(Socket, _key, _version, _subversion, out error);
            if (error > 0) Error = error;
            else IsSearching = true;
        }

        public void Shutdown()
        {
            WakeNet.Log("WakeDiscovery::Shutdown()");
            if (IsBroadcasting)
            {
                NetworkTransport.StopBroadcastDiscovery();
                IsBroadcasting = false;
            }
            if (IsSearching)
            {
                IsSearching = false;
            }
        }

        internal override void ProcessIncomingEvent(NetworkEventType netEvent, int connectionId, int channelId, byte[] buffer, int dataSize)
        {
            Debug.Log(NetworkTransport.IsBroadcastDiscoveryRunning());
            if(netEvent != NetworkEventType.BroadcastEvent) return;
            
            WakeNet.Log(dataSize);
        }
    }
}