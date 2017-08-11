#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#endregion

namespace Wake
{
    public sealed class WakeDiscovery : WakeObject
    {
        private readonly Dictionary<string, Result> _foundGames = new Dictionary<string, Result>();
        
        private readonly int _key;
        private readonly int _port;
        private readonly int _subversion;
        private readonly int _version;
        private readonly float _interval;


        public WakeDiscovery(int port, int key, int version, int subversion, float interval = 1f)
        {
            WakeNet.Log("WakeDiscovery::Ctor()");
            
            _port = port;
            _key = key;
            _version = version;
            _subversion = subversion;
            _interval = interval;
        }

        public bool IsBroadcasting { get; private set; }
        public bool IsSearching { get; private set; }

        public ReadOnlyCollection<Result> FoundGames
        {
            get { return new ReadOnlyCollection<Result>(_foundGames.Values.ToList()); }
        }

        public void Broadcast(string broadcastMessage, int port)
        {
            if (IsBroadcasting) return;

            Socket = WakeNet.AddSocket(1);
            WakeNet.Log(string.Format("WakeDiscovery:{0}:Broadcast()", Socket));
            WakeNet.RegisterSocket(Socket);

            var sendInfo = new GameResult
            {
                DeviceId = SystemInfo.deviceUniqueIdentifier,
                Message = broadcastMessage,
                Port = port
            };

            var data = Encoding.UTF8.GetBytes(JsonUtility.ToJson(sendInfo, true));
            byte error;
            NetworkTransport.StartBroadcastDiscovery(Socket, _port, _key, _version, _subversion, data, data.Length, 1000, out error);
            if (error > 0) Error = error;
            else IsBroadcasting = true;
        }

        public void Search()
        {
            if (IsSearching) return;

            Socket = WakeNet.AddSocket(1, _port);
            WakeNet.Log(string.Format("WakeDiscovery:{0}:Search()", Socket));
            WakeNet.RegisterSocket(Socket);

            byte error;
            NetworkTransport.SetBroadcastCredentials(Socket, _key, _version, _subversion, out error);
            if (error > 0) Error = error;
            else IsSearching = true;
        }

        public void Shutdown()
        {
            WakeNet.Log(string.Format("WakeDiscovery:{0}:Shutdown()", Socket));
            if (IsBroadcasting)
            {
                NetworkTransport.StopBroadcastDiscovery();
                IsBroadcasting = false;
            }
            if (IsSearching)
                IsSearching = false;
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
                _foundGames.Add(gameResult.DeviceId, new Result { Host = host, Port = gameResult.Port, GameResult = gameResult });
                _foundGames[gameResult.DeviceId].Routine = WakeNet.InvokeAt(() => { _foundGames.Remove(gameResult.DeviceId); }, Time.unscaledTime + _interval * 2);
            }
            else
            {
                _foundGames[gameResult.DeviceId].Host = host;
                _foundGames[gameResult.DeviceId].Port = gameResult.Port;
                _foundGames[gameResult.DeviceId].GameResult.Message = gameResult.Message;
                WakeNet.StopRoutine(_foundGames[gameResult.DeviceId].Routine);
                _foundGames[gameResult.DeviceId].Routine = WakeNet.InvokeAt(() => { _foundGames.Remove(gameResult.DeviceId); }, Time.unscaledTime + _interval * 2);
            }
        }
        
        public sealed class Result
        {
            public GameResult GameResult;
            public string Host;
            public int Port;
            [NonSerialized] public Coroutine Routine;
        }

        [Serializable]
        public sealed class GameResult
        {
            public string DeviceId;
            public string Message;
            public int Port;
        }
    }
}