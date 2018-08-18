using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Wake;
using Wake.Logger;

public class DiscoveryServer : MonoBehaviour
{
    [Header("Settigns")] 
    [SerializeField] private int _port = 9999;
    [SerializeField] private int _serverPort = 9999;
    [SerializeField] private int _key = 1;
    [SerializeField] private int _version = 1;
    [SerializeField] private int _subversion = 1;
    [SerializeField] private Color _broadcastColor = Color.red;

    private WakeDiscovery _discovery;


	// Use this for initialization
	IEnumerator Start ()
	{
	    var c = WakeNetConfig.Default;
	    c.LogLevel = LogLevel.Debug;
	    WakeNet.Init(c);

	    while (!WakeNet.Initialized)
	    {
	        yield return new WaitForEndOfFrame();
	    }

	    _discovery = WakeNet.CreateDiscovery(_port, _key, _version, _subversion);
        _discovery.Broadcast(ColorUtility.ToHtmlStringRGBA(_broadcastColor), _serverPort);

	    GetComponent<Image>().color = _broadcastColor;
	}
}
