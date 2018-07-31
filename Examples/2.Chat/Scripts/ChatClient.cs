using System;
using System.Collections;
using Assets.Plugins.Examples._2.Chat.Scripts.MessageTypes;
using UnityEngine;
using UnityEngine.UI;
using Wake;
using Wake.Protocol.Proxy;

public class ChatClient : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private string _serverHost = "127.0.0.1";
    [SerializeField] private int _serverPort = 9999;
    
    [Header("View")]
    public Text ClientLabel;
    public Transform MessagesRoot;
    public GameObject MessagePrefab;
    public InputField Input;
    public Button SendButton;

    private WakeClient _client;
    private Proxy<ChatMessagePacket, ChatMessagePacket> _proxy;
    private ProxyReceiver<ServerIdentityPacket> _identity;

    private int _clientId;
    
	IEnumerator Start ()
	{
	    while (!WakeNet.Initialized)
	    {
	        yield return new WaitForEndOfFrame();
	    }

	    SendButton.interactable = false;

	    _client = WakeNet.CreateClient();
	    _proxy = _client.AddProxy<ChatMessagePacket, ChatMessagePacket>("msg", 0, true);
	    _identity = _client.AddProxyReceiver<ServerIdentityPacket>("sid", 0, false);

	    _identity.Received += OnIdentityReceived;
	    _proxy.Received += OnMessageReceived;
        
        _client.Connect(_serverHost, _serverPort);
	    _client.Connected += OnClientConnected;
	}

    private void OnClientConnected()
    {
        SendButton.interactable = true;
        SendButton.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(Input.text))
            {
                _proxy.Send(new ChatMessagePacket($"User №{_clientId}", Input.text));
            }
        });
    }

    private void OnIdentityReceived(ServerIdentityPacket message, int connectionid)
    {
        _clientId = message.Data;
        ClientLabel.text = "Wake Chat Client №" + _clientId;
    }

    private void OnMessageReceived(ChatMessagePacket message, int connectionid)
    {
        var messageGO = Instantiate(MessagePrefab, MessagesRoot, false);
        messageGO.GetComponent<Text>().text = $"[{DateTime.Now:t}] ({message.Sender}) : {message.Data}";
        messageGO.SetActive(true);
    }
}
