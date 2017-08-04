#region Usings

using System;
using UnityEngine.Networking;

#endregion

namespace WakeNet.Internal
{
    /// <summary>
    ///     The net client handles connecting to a server as well as sending and receiving messages.
    /// </summary>
    internal class NetClient
    {
        public int mConnection = -1;
        public bool mIsConnected;
        public int mPort = -1;
        public string mServerIP;

        public int mSocket = -1;
        public NetEventHandler OnMessage = null;
        public Action OnSend = null;

        internal NetClient(int socket, int connection)
        {
            mSocket = socket;
            mConnection = connection;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="NetClient" /> class.
        /// </summary>
        public NetClient()
        {
            var csocket = NetworkTransport.AddHost(Config.GetHostTopology(1));

            if (!NetUtils.IsSocketValid(csocket))
                NetUtils.Log("NetManager::CreateClient() returned an invalid socket ( " + csocket + " )");

            mSocket = csocket;
        }

        /// <summary>
        ///     Connect the specified ip and port.
        /// </summary>
        /// <param name="ip">Ip.</param>
        /// <param name="port">Port.</param>
        public bool Connect(string ip, int port)
        {
            byte error;
            mConnection = NetworkTransport.Connect(mSocket, ip, port, 0, out error);

            if (NetUtils.IsNetworkError(this, error)) return false;

            mServerIP = ip;
            mPort = port;

            return true;
        }

        /// <summary>
        ///     Disconnect the client from the server.
        /// </summary>
        public bool Disconnect()
        {
            if (!mIsConnected)
            {
                NetUtils.LogWarning("NetClient::Disconnect() Failed with reason 'Not connected to server!");
                return false;
            }

            byte error;

            NetworkTransport.Disconnect(mSocket, mConnection, out error);

            if (NetUtils.IsNetworkError(this, error))
                return false;

            mIsConnected = false;

            return true;
        }

        /// <summary>
        ///     Sends the stream.
        /// </summary>
        /// <returns><c>true</c>, if stream was sent, <c>false</c> otherwise.</returns>
        public bool Send(byte[] data, int channel)
        {
            byte error;
            NetworkTransport.Send(mSocket, mConnection, channel, data, data.Length, out error);

            NetUtils.IsNetworkError(this, error);

            return true;
        }
    }
}