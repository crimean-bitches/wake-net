#region Usings

using System;

#endregion

namespace WakeNet.Protocol.Internal
{
    [Serializable]
    internal class Packet
    {
        public byte[] Data;
        public ushort ProxyId;
    }
}