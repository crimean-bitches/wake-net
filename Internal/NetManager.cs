#region Usings

using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Networking;

#endregion

namespace WakeNet.Internal
{
    /// <summary>
    ///     The NetManager is responsible for creating our client and server sockets and housing our messaging queue/delegate
    ///     system.
    /// </summary>
    internal class NetManager
    {
        // True if Init has ran.
        public static bool Initialized { get; private set; }

        public static ReadOnlyCollection<NetServer> Servers
        {
            get { return new ReadOnlyCollection<NetServer>(_servers); }
        }

        public static ReadOnlyCollection<NetClient> Clients
        {
            get { return new ReadOnlyCollection<NetClient>(_clients); }
        }

        // Lists to hold our clients
        private static readonly List<NetServer> _servers = new List<NetServer>();

        private static readonly List<NetClient> _clients = new List<NetClient>();

        /// <summary>
        ///     Initialize our low level network APIs.
        /// </summary>
        public static void Init()
        {
            NetworkTransport.Init(Config.GetClobalConfig());
            Initialized = true;

            NetManagerHandler.Instance.Init();
        }

        public static void Shutdown()
        {
            // Kill all clients
            for (var i = 0; i < _clients.Count; i++)
                DestroyClient(_clients[i]);

            // Disconnect and destroy all servers
            for (var i = 0; i < _servers.Count; i++)
                DestroyServer(_servers[i]);

            NetworkTransport.Shutdown();
            Initialized = false;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="NetServer" /> class. We can simulate real world network conditions by
        ///     using the simMinTimeout/simMaxTimeout params
        ///     to simulate connection lag.
        /// </summary>
        /// <returns>The server object.</returns>
        /// <param name="maxConnections">Max connections.</param>
        /// <param name="port">Port.</param>
        /// <param name="simMinLatency">Minimum latency to simulate on the server.</param>
        /// <param name="simMaxLatency">Maximum latency to simulate on the server.</param>
        public static NetServer CreateServer(int maxConnections, int port, int simMinLatency = 0, int simMaxLatency = 0)
        {
            var s = new NetServer(maxConnections, port, simMinLatency, simMaxLatency);

            // If we were successful in creating our server and it is unique
            if (s.mIsRunning && _servers.Contains(s) != true)
                _servers.Add(s);

            return s;
        }

        /// <summary>
        ///     Destroys the server.
        /// </summary>
        /// <returns><c>true</c>, if server was destroyed, <c>false</c> otherwise.</returns>
        /// <param name="s">NetServer to destroy</param>
        public static bool DestroyServer(NetServer s)
        {
            if (_servers.Contains(s) == false)
            {
                NetUtils.Log("NetManager::DestroyServer( " + s.mSocket + ") - Server does not exist!");
                return false;
            }

            s.DisconnectAllClients();

            NetworkTransport.RemoveHost(s.mSocket);

            _servers.Remove(s);

            return true;
        }

        /// <summary>
        ///     Create a client that is ready to connect with a server.
        /// </summary>
        /// <returns>The client.</returns>
        public static NetClient CreateClient()
        {
            if (!Initialized)
            {
                NetUtils.Log(
                    "NetManager::CreateServer( ... ) - NetManager was not initialized. Did you forget to call NetManager.Init()?");
                return null;
            }

            var c = new NetClient();

            if (_clients.Contains(c) != true)
                _clients.Add(c);

            return c;
        }

        /// <summary>
        ///     Destroys specified client.
        /// </summary>
        /// <returns><c>true</c>, if client was destroyed, <c>false</c> otherwise.</returns>
        /// <param name="c">NetClient object to destroy</param>
        public static bool DestroyClient(NetClient c)
        {
            if (_clients.Contains(c) == false)
            {
                NetUtils.Log("NetManager::DestroyClient( " + c.mSocket + ") - Client does not exist!");
                return false;
            }

            c.Disconnect();

            NetworkTransport.RemoveHost(c.mSocket);

            _clients.Remove(c);

            return true;
        }

        /// <summary>
        ///     Reads network events and delegates how they are used
        /// </summary>
        public static void PollEvents()
        {
            // If nothing is running, why bother
            if (_servers.Count < 1 && _clients.Count < 1)
                return;

            int recHostId;
            int connectionId;
            int channelId;
            int dataSize;
            var buffer = new byte[1024];
            byte error;

            var networkEvent = NetworkEventType.DataEvent;

            // Process network events for n clients and n servers
            while (Initialized && networkEvent != NetworkEventType.Nothing)
            {
                var i = -1; // Index for netserver in mservers

                networkEvent = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, buffer, 1024, out dataSize, out error);

                // Route message to our server delegate
                i = _servers.FindIndex(x => x.mSocket == recHostId);
                if (i != -1) _servers[i].OnMessage(networkEvent, connectionId, channelId, buffer, dataSize);

                // Route message to our client delegate
                // Client Connect Event
                i = _clients.FindIndex(c => c.mSocket.Equals(recHostId));
                if (i != -1) _clients[i].OnMessage(networkEvent, connectionId, channelId, buffer, dataSize);

                // invoke handler which allows to send all waiting data on server and client sides
                for (int c = 0; c < _clients.Count; c++)
                    _clients[c].OnSend();
                for (int s = 0; s < _servers.Count; s++)
                    _servers[s].OnSend();

                switch (networkEvent)
                {
                    // Nothing
                    case NetworkEventType.Nothing:
                        break;

                    // Connect
                    case NetworkEventType.ConnectEvent:

                        // Server Connect Event
                        i = _servers.FindIndex(s => s.mSocket.Equals(recHostId));

                        if (i != -1) _servers[i].AddClient(connectionId);

                        // Client Connect Event
                        i = _clients.FindIndex(c => c.mSocket.Equals(recHostId));
                        if (i != -1) _clients[i].mIsConnected = true; // Set client connected to true

                        break;

                    // Data 
                    case NetworkEventType.DataEvent:

                        // Server received data
                        i = _servers.FindIndex(x => x.mSocket == recHostId);
                        if (i != -1)
                        {
                            // Handles via OnMessage
                        }

                        // Client Received Data
                        i = _clients.FindIndex(c => c.mSocket.Equals(recHostId));
                        if (i != -1)
                        {
                            // Handles via OnMessage
                        }
                        break;

                    // Disconnect
                    case NetworkEventType.DisconnectEvent:

                        // Server Disconnect Event
                        i = _servers.FindIndex(x => x.mSocket == recHostId);
                        if (i != -1) _servers[i].RemoveClient(connectionId);

                        // Client Disconnect Event
                        i = _clients.FindIndex(c => c.mSocket.Equals(recHostId));
                        if (i != -1) _clients[i].mIsConnected = false; // Set client connected to true

                        break;
                }
            }
        }
    }
}