using UnityEngine.Networking;

namespace WakeNet.Internal
{
    /// <summary>
    ///     Our delegate that gets called to handle processing data
    /// </summary>
    public delegate void NetEventHandler(NetworkEventType net, int connectionId, int channelId, byte[] buffer, int datasize);

}