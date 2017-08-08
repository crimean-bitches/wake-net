#region Usings

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Helper;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

#endregion

namespace Wake
{
    public sealed class WakeNet : MonoBehaviour
    {
        #region Singleton

        private static WakeNet _instance;

        public static WakeNet Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new GameObject("WakeNet").AddComponent<WakeNet>();
                DontDestroyOnLoad(_instance.gameObject);

                return _instance;
            }
        }

        #endregion

        #region Instance

        private float _executeTime;
        private Coroutine _pollRoutine;

        private void Start()
        {
            if (_instance._pollRoutine == null)
                _instance._pollRoutine = _instance.StartCoroutine(_instance.PollRoutine());
        }

        private IEnumerator PollRoutine()
        {
            while (Initialized)
            {
                var ts = GameTime.Now;
                PollEvents();
                _executeTime = GameTime.Now - ts;
                yield return new WaitForSeconds(1f / _config.ReceiveRate - _executeTime);
            }
        }

        private void PollEvents()
        {
            // If nothing is running, why bother
            if (_servers.Count < 1 && _clients.Count < 1 && _discoveries.Count < 1)
                return;

            var recHostId = -1;
            var connectionId = -1;
            int channelId;
            int dataSize;
            var buffer = new byte[_config.ConnectionConfig.PacketSize];
            byte error;

            NetworkEventType networkEvent;
            // Process network events for n clients and n servers
            do
            {
                var i = -1; // Index for WakeObject in collections to process

                networkEvent = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, buffer,
                    _config.ConnectionConfig.PacketSize, out dataSize, out error);

                Log(networkEvent);

                // Route message to our server delegate
                i = _servers.FindIndex(x => x.Socket == recHostId);
                if (i != -1) _servers[i].ProcessIncomingEvent(networkEvent, connectionId, channelId, buffer, dataSize);

                // Route message to our client delegate
                // Client Connect Event
                i = _clients.FindIndex(c => c.Socket.Equals(recHostId));
                if (i != -1) _clients[i].ProcessIncomingEvent(networkEvent, connectionId, channelId, buffer, dataSize);

                // Route message to our broadcast delegate
                for (var d = 0; d < _discoveries.Count; d++)
                    _discoveries[d].ProcessIncomingEvent(networkEvent, connectionId, channelId, buffer, dataSize);

                // invoke handler which allows to send all waiting data on server and client sides
                for (var c = 0; c < _clients.Count; c++) _clients[c].ProcessOutgoingEvents();
                for (var s = 0; s < _servers.Count; s++) _servers[s].ProcessOutgoingEvents();
            } while (Initialized && networkEvent != NetworkEventType.Nothing);
        }

        #endregion

        #region Static

        // True if Init has ran.
        public static bool Initialized { get; private set; }

        // Wake elements accessors
        public static ReadOnlyCollection<WakeServer> Servers => new ReadOnlyCollection<WakeServer>(_servers);

        public static ReadOnlyCollection<WakeClient> Clients => new ReadOnlyCollection<WakeClient>(_clients);

        public static ReadOnlyCollection<WakeDiscovery> Discoveries =>
            new ReadOnlyCollection<WakeDiscovery>(_discoveries);

        // Lists to hold our clients, servers and discoveries
        private static readonly List<int> _sockets = new List<int>();

        private static readonly List<WakeServer> _servers = new List<WakeServer>();
        private static readonly List<WakeClient> _clients = new List<WakeClient>();
        private static readonly List<WakeDiscovery> _discoveries = new List<WakeDiscovery>();

        private static WakeNetConfig _config;

        /// <summary>
        ///     Initialize our low level network APIs.
        /// </summary>
        public static void Init(WakeNetConfig config)
        {
            if (Initialized) return;
            _config = config ?? WakeNetConfig.Default;

            Network.logLevel = config.LogLevel;
            Instance.Start();
            NetworkTransport.Init(_config.GlobalConfig);
            Initialized = true;
        }

        #region Server

        /// <summary>
        ///     Initializes a new instance of the <see cref="WakeServer" /> class. We can simulate real world network conditions by
        ///     using the simMinTimeout/simMaxTimeout params
        ///     to simulate connection lag.
        /// </summary>
        /// <returns>The server object.</returns>
        /// <param name="maxConnections">Max connections.</param>
        /// <param name="port">Port.</param>
        /// <param name="simMinLatency">Minimum latency to simulate on the server.</param>
        /// <param name="simMaxLatency">Maximum latency to simulate on the server.</param>
        public static WakeServer CreateServer(int maxConnections, int port, int simMinLatency = 0,
            int simMaxLatency = 0)
        {
            if (!Initialized)
            {
                Log(WakeError.NotInitialized);
                return null;
            }

            var server = new WakeServer(maxConnections, port, simMinLatency, simMaxLatency);

            // If we were successful in creating our server and it is unique
            if (server.IsConnected && !_servers.Contains(server))
            {
                _servers.Add(server);
                return server;
            }

            RemoveSocket(server.Socket);
            return server;
        }

        /// <summary>
        ///     Destroys the instance of <see cref="WakeServer" />.
        /// </summary>
        /// <param name="server">NetServer to destroy</param>
        /// <returns><c>true</c>, if server was destroyed, <c>false</c> otherwise.</returns>
        public static void DestroyServer(WakeServer server)
        {
            if (_servers.Contains(server) == false)
            {
                Log(WakeError.ServerNotExists);
                return;
            }

            server.DisconnectAllClients();
            RemoveSocket(server.Socket);
            _servers.Remove(server);
        }

        #endregion

        #region Client

        /// <summary>
        ///     Create a client that is ready to connect with a server.
        /// </summary>
        /// <returns> The <see cref="WakeClient" /> instance.</returns>
        public static WakeClient CreateClient()
        {
            if (!Initialized)
            {
                Log(WakeError.NotInitialized);
                return null;
            }

            var client = new WakeClient();

            // If we were successful in creating our client and it is unique
            if (!_clients.Contains(client))
            {
                _clients.Add(client);

                return client;
            }

            RemoveSocket(client.Socket);
            return null;
        }

        /// <summary>
        ///     Destroys specified client.
        /// </summary>
        /// <returns><c>true</c>, if client was destroyed, <c>false</c> otherwise.</returns>
        /// <param name="client"><see cref="WakeClient" /> object to destroy. </param>
        public static void DestroyClient(WakeClient client)
        {
            if (!_clients.Contains(client))
            {
                Log(WakeError.ClientNotExists);
                return;
            }

            client.Disconnect();
            RemoveSocket(client.Socket);
            _clients.Remove(client);
        }

        #endregion

        #region Discovery

        /// <summary>
        ///     Create a discovery, that ready to broadcast or receive events.
        /// </summary>
        /// <param name="port"> Listen or broadcast port. </param>
        /// <param name="key"> Key parameter to specify game veresion. </param>
        /// <param name="version"> Major version of application. </param>
        /// <param name="subversion"> Minor version of application. </param>
        /// <returns></returns>
        public static WakeDiscovery CreateDiscovery(int port, int key, int version, int subversion)
        {
            if (!Initialized)
            {
                Log(WakeError.NotInitialized);
                return null;
            }

            var discovery = new WakeDiscovery(port, key, version, subversion);

            if (!_discoveries.Contains(discovery))
            {
                _discoveries.Add(discovery);
                return discovery;
            }

            RemoveSocket(discovery.Socket);
            return null;
        }

        /// <summary>
        ///     Destroys specified discovery instance.
        /// </summary>
        /// <param name="discovery"> <see cref="WakeDiscovery" /> instance to destroy. </param>
        public static void DestroyDiscovery(WakeDiscovery discovery)
        {
            if (!_discoveries.Contains(discovery))
            {
                Log(WakeError.DiscoveryNotExists);
                return;
            }
            
            discovery.Shutdown();
            RemoveSocket(discovery.Socket);
            _discoveries.Remove(discovery);
        }

        #endregion

        #region Utils

        internal static int AddSocket(int maxConnections, bool websocket = false)
        {
            if (!Initialized) Log(WakeError.NotInitialized);

            int socket;

            var ht = new HostTopology(_config.ConnectionConfig, maxConnections);
            if (websocket)
            {
                Log(WakeError.NotImplemented);
                return -1;
            }

            socket = NetworkTransport.AddHost(ht);

            if (!IsValidSocketToCreate(socket))
            {
                Log(WakeError.InvalidHostCreated);
                return -1;
            }

            _sockets.Add(socket);
            return socket;
        }

        internal static int AddSocket(int maxConnections, int port, bool websocket = false)
        {
            return AddSocket(maxConnections, 0, 0, port, websocket);
        }

        internal static int AddSocket(int maxConnections, int simMinTimeout, int simMaxTimeout, int port,
            bool websocket = false)
        {
            if (!Initialized) Log(WakeError.NotInitialized);

            int socket;

            var ht = new HostTopology(_config.ConnectionConfig, maxConnections);
            if (websocket)
            {
                Log(WakeError.NotImplemented);
                return -1;
            }
            if (simMinTimeout > 0 || simMaxTimeout > 0)
                socket = NetworkTransport.AddHostWithSimulator(ht, simMinTimeout, simMaxTimeout, port);
            else
                socket = NetworkTransport.AddHost(ht, port);

            if (!IsValidSocketToCreate(socket))
            {
                Log(WakeError.InvalidHostCreated);
                return -1;
            }

            _sockets.Add(socket);
            return socket;
        }

        internal static void RegisterSocket(int socket)
        {
            if (!Initialized) Log(WakeError.NotInitialized);
            if (!IsValidSocketToCreate(socket)) return;

            _sockets.Add(socket);
        }

        internal static void RemoveSocket(int socket)
        {
            if (!Initialized) Log(WakeError.NotInitialized);
            if (!IsValidSocketToRemove(socket)) Log(WakeError.NotInitialized);

            NetworkTransport.RemoveHost(socket);
            _sockets.Remove(socket);
        }

        #endregion

        #region Internal Utils

        private static bool IsValidSocketToCreate(int sock)
        {
            if (sock < 0) return false;
            return !_sockets.Contains(sock);
        }

        private static bool IsValidSocketToRemove(int sock)
        {
            if (sock < 0) return false;
            return _sockets.Contains(sock);
        }

        internal static void StopRoutine(Coroutine routine)
        {
            Instance.StopCoroutine(routine);
        }
        internal static Coroutine InvokeAt(UnityAction action, float time)
        {
            return Instance.StartCoroutine(WaitUntilAndInvoke(action, time));
        }

        private static IEnumerator WaitUntilAndInvoke(UnityAction action, float time)
        {
            while (time > Time.unscaledTime)
                yield return null;
            if (action != null) action();
        }

        internal static void Log(object message)
        {
            Debug.Log(message);
        }

        internal static void Log(string message)
        {
            Debug.Log(message);
        }

        internal static void Log(string message, params object[] args)
        {
            Debug.LogFormat(message, args);
        }

        #endregion

        #endregion
    }
}