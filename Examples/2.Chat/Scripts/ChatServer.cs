using System.Collections;
using Assets.Plugins.Examples._2.Chat.Scripts.MessageTypes;
using UnityEngine;
using Wake;
using Wake.Logger;
using Wake.Protocol.Proxy;

public class ChatServer : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private int _serverPort = 9999;
    [SerializeField] private int _serverMaxConnections = 9999;

    private WakeServer _server;
    private Proxy<ChatMessagePacket, ChatMessagePacket> _proxy;
    private ProxySender<ServerIdentityPacket> _identity;


	IEnumerator Start ()
	{
	    var c = WakeNetConfig.Default;
	    c.LogLevel = LogLevel.Debug;
		WakeNet.Init(c);

	    while (!WakeNet.Initialized)
	    {
            yield return new WaitForEndOfFrame();
	    }

	    _server = WakeNet.CreateServer(_serverMaxConnections, _serverPort);
	    _server.ClientConnected += OnClientConnected;

	    _proxy = _server.AddProxy<ChatMessagePacket, ChatMessagePacket>("msg", 0);

	    _proxy.Received += OnMessageReceived;
	}

    private void OnClientConnected(WakeClient client)
    {
        var identity = client.AddProxySender<ServerIdentityPacket>("sid", 0, false);

        identity.Send(new ServerIdentityPacket(client.ConnectionId));

        _proxy.Send(new ChatMessagePacket("Server", "User №" + client.ConnectionId + " joined chat..."));
    }

    private void OnMessageReceived(ChatMessagePacket message, int connectionid)
    {
        _proxy.Send(message);
    }
}
