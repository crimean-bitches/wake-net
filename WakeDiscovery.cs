#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Helper;
using UnityEngine;
using UnityEngine.Networking;

#endregion

namespace Wake
{
    public sealed class WakeDiscovery : WakeObject
    {
        private readonly Dictionary<string, Result> _foundGames = new Dictionary<string, Result>();

        private readonly HostTopology _hostTopology;
        private readonly int _key;
        private readonly int _port;
        private readonly int _subversion;
        private readonly int _version;


        public WakeDiscovery(int port, int key, int version, int subversion)
        {
            WakeNet.Log("WakeDiscovery::Ctor()");

            var connectionConfig = new ConnectionConfig();
            connectionConfig.AddChannel(QosType.Unreliable);
            _hostTopology = new HostTopology(connectionConfig, 1);

            _port = port;
            _key = key;
            _version = version;
            _subversion = subversion;
        }

        public bool IsBroadcasting { get; private set; }
        public bool IsSearching { get; private set; }

        public ReadOnlyCollection<Result> FoundGames => new ReadOnlyCollection<Result>(_foundGames.Values.ToList());

        public void Broadcast(string broadcastMessage, string password = "")
        {
            WakeNet.Log("WakeDiscovery::Broadcast()");
            if (IsBroadcasting) return;

            Socket = NetworkTransport.AddHost(_hostTopology);
            WakeNet.RegisterSocket(Socket);

            var sendInfo = new GameResult
            {
                DeviceId = SystemInfo.deviceUniqueIdentifier,
                Message = broadcastMessage,
                PasswordHash = string.IsNullOrEmpty(password) ? "" : MD5.Hash(password),
                Timestamp = NetworkTransport.GetNetworkTimestamp()
            };

            var data = Encoding.UTF8.GetBytes(JsonUtility.ToJson(sendInfo, true));
            byte error;
            NetworkTransport.StartBroadcastDiscovery(Socket, _port, _key, _version, _subversion, data, data.Length,
                1000, out error);
            if (error > 0) Error = error;
            else IsBroadcasting = true;
        }

        public void Search()
        {
            WakeNet.Log("WakeDiscovery::Search()");
            if (IsSearching) return;

            Socket = NetworkTransport.AddHost(_hostTopology, _port);
            WakeNet.RegisterSocket(Socket);

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
                IsSearching = false;
            WakeNet.RemoveSocket(Socket);
        }

        internal override void ProcessIncomingEvent(NetworkEventType netEvent, int connectionId, int channelId,
            byte[] buffer, int dataSize)
        {
            if (netEvent != NetworkEventType.BroadcastEvent) return;

            byte error;
            NetworkTransport.GetBroadcastConnectionMessage(Socket, buffer, buffer.Length, out dataSize, out error);
            if (error > 0)
            {
                Error = error;
                return;
            }
            string host;
            int port;
            NetworkTransport.GetBroadcastConnectionInfo(Socket, out host, out port, out error);
            if (error > 0)
            {
                Error = error;
                return;
            }

            var data = new byte[dataSize];
            Buffer.BlockCopy(buffer, 0, data, 0, dataSize);
            WakeNet.Log(Encoding.UTF8.GetString(data));
            var gameResult = JsonUtility.FromJson<GameResult>(Encoding.UTF8.GetString(data));

            if (!_foundGames.ContainsKey(gameResult.DeviceId))
            {
                _foundGames.Add(gameResult.DeviceId, new Result {Host = host, Port = port, GameResult = gameResult});
            }
            else
            {
                _foundGames[gameResult.DeviceId].GameResult.Message = gameResult.Message;
                _foundGames[gameResult.DeviceId].GameResult.PasswordHash = gameResult.PasswordHash;
                _foundGames[gameResult.DeviceId].GameResult.Timestamp = gameResult.Timestamp;
            }

            // TODO clean games which were not bradcasting
        }

        [Serializable]
        public sealed class Result
        {
            public GameResult GameResult;
            public string Host;
            public int Port;
        }

        [Serializable]
        public sealed class GameResult
        {
            public string DeviceId;
            public string Message;
            public string PasswordHash;
            public int Timestamp;
        }
    }
}