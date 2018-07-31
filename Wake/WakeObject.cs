#region Usings

using UnityEngine.Networking;

#endregion

namespace Wake
{
    public abstract class WakeObject
    {
        public int Socket { get; protected set; }
        public bool IsConnected { get; protected set; }
        public byte Error { get; protected set; }

        internal abstract void ProcessIncomingEvent(NetworkEventType netEvent, int connectionId, int channelId,
            byte[] buffer, int dataSize);
    }
}