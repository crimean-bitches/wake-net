#region Usings

using System;
using System.Collections.Generic;
using UnityEngine.Networking;

#endregion

namespace WakeNet.Internal
{
    /// <summary>
    ///     The NetServer handles client tracking, broadcasting and sending/receiving messages from clients.
    /// </summary>
    public class NetServer
    {
        public List<int> mClients = new List<int>();
        public bool mIsRunning;
        public int mPort = -1;

        public int mSocket = -1;
        public NetEventHandler OnMessage = null;
        public Action OnSend = null;

        /// <summary>
        ///     Initializes a new instance of the <see cref="NetServer" /> class. We can simulate real world network conditions by
        ///     using the simMinTimeout/simMaxTimeout params
        ///     to simulate connection lag.
        /// </summary>
        /// <param name="maxConnections">Max connections.</param>
        /// <param name="port">Port.</param>
        /// <param name="simMinTimeout">Minimum lag timeout to simulate in ms. Set to zero for none.</param>
        /// <param name="simMaxTimeout">Maximum lag timeout to simulate in ms. Set to zero for none.</param>
        public NetServer(int maxConnections, int port, int simMinTimeout = 0, int simMaxTimeout = 0)
        {
            if (!NetManager.Initialized)
            {
                NetUtils.LogWarning("NetServer( ... ) - NetManager was not initialized. Did you forget to call NetManager.Init()?");
                return;
            }

            if (simMinTimeout != 0 || simMaxTimeout != 0)
                mSocket = NetworkTransport.AddHostWithSimulator(Config.GetHostTopology(maxConnections), simMinTimeout,
                    simMaxTimeout, port);
            else mSocket = NetworkTransport.AddHost(Config.GetHostTopology(maxConnections), port);

            mPort = port;

            if (!NetUtils.IsSocketValid(mSocket))
                NetUtils.LogWarning("NetServer::NetServer( " + maxConnections + " , " + port + " ) returned an invalid socket ( " + mSocket + " )");

            mIsRunning = true;
        }

        /// <summary>
        ///     Broadcast a stream to all connected clients.
        /// </summary>
        /// <param name="o">The object to serialize and stream.</param>
        /// <param name="buffsize">Max buffer size of object after being serialized.</param>
        /// <param name="channel">Channel to broadcast on.</param>
        public void BroadcastStream(byte[] data, int channel)
        {
            byte error;

            foreach (var element in mClients)
            {
                NetworkTransport.Send(mSocket, element, channel, data, data.Length, out error);

                NetUtils.IsNetworkError(this, error);
            }
        }

        /// <summary>
        ///     Send a stream to a single connected client.
        /// </summary>
        /// <returns><c>true</c>, if stream was sent, <c>false</c> otherwise.</returns>
        /// <param name="channel">Channel to broadcast on.</param>
        /// <param name="data">Data to send.</param>
        /// <param name="connId">Connection ID of client to broadcast to.</param>
        public bool Send(byte[] data, int connId, int channel)
        {
            byte error;
            NetworkTransport.Send(mSocket, connId, channel, data, data.Length, out error);

            return !NetUtils.IsNetworkError(this, error);
        }

        /// <summary>
        ///     Disconnects a client via specified id.
        /// </summary>
        /// <returns><c>true</c>, if client was disconnected, <c>false</c> otherwise.</returns>
        public bool DisconnectClient(int connId)
        {
            if (!HasClient(connId))
            {
                NetUtils.LogWarning("NetServer::DisconnectClient( " + connId + " ) Failed with reason 'Client with id does not exist!'");
                return false;
            }

            byte error;
            NetworkTransport.Disconnect(mSocket, connId, out error);
            
            return !NetUtils.IsNetworkError(this, error);
        }

        /// <summary>
        ///     Disconnects all clients.
        /// </summary>
        public void DisconnectAllClients()
        {
            foreach (var c in mClients) DisconnectClient(c);
        }
        
        /// <summary>
        ///     Adds a client connection to the server after making sure it will be unique.
        /// </summary>
        /// <returns><c>true</c>, if client was added, <c>false</c> otherwise.</returns>
        /// <param name="connId">Connection ID</param>
        public bool AddClient(int connId)
        {
            if (mClients.Contains(connId))
            {
                NetUtils.LogWarning("NetServer::AddClient( " + connId + " ) - Id already exists!");
                return false;
            }

            mClients.Add(connId);
            return true;
        }

        /// <summary>
        ///     Removes a client connection from the server after making sure that it exists.
        /// </summary>
        /// <returns><c>true</c>, if client was removed, <c>false</c> otherwise.</returns>
        /// <param name="connId">Connection identifier.</param>
        public bool RemoveClient(int connId)
        {
            if (!mClients.Exists(element => element == connId))
            {
                NetUtils.LogWarning("NetServer::RemoveClient( " + connId + " ) - Client not connected!");
                return false;
            }

            mClients.Remove(connId);
            return true;
        }

        /// <summary>
        ///     Determines whether this instance has a client of the specified connId.
        /// </summary>
        /// <returns><c>true</c> if this instance has a client of the specified connId; otherwise, <c>false</c>.</returns>
        /// <param name="connId">Conn identifier.</param>
        public bool HasClient(int connId)
        {
            return mClients.Exists(element => element == connId);
        }
    }
}