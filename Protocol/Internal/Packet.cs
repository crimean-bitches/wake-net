#region Usings

using System;

#endregion

namespace Wake.Protocol.Internal
{
    [Serializable]
    internal class Packet
    {
        public byte[] Data;
        public string ProxyId;
        public bool Server;
    }
}