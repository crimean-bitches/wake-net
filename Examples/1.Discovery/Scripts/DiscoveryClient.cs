using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wake;

public class DiscoveryClient : MonoBehaviour
{
    [Header("Settigns")] [SerializeField] private int _port = 9999;
    [SerializeField] private int _serverPort = 9999;
    [SerializeField] private int _key = 1;
    [SerializeField] private int _version = 1;
    [SerializeField] private int _subversion = 1;

    private WakeDiscovery _discovery;
    
    private IEnumerator Start()
    {
        var c = WakeNetConfig.Default;
        c.LogLevel = NetworkLogLevel.Full;
        WakeNet.Init(c);

        while (!WakeNet.Initialized)
        {
            yield return new WaitForEndOfFrame();
        }

        _discovery = WakeNet.CreateDiscovery(_port, _key, _version, _subversion);
        _discovery.Search();
    }

    private void Update()
    {
        if (_discovery.FoundGames.Count <= 0)
        {
            return;
        }

        var i = (int) Time.time % _discovery.FoundGames.Count;
        SetColor(i);
    }

    private void SetColor(int i)
    {
        Color color;
        ColorUtility.TryParseHtmlString(_discovery.FoundGames[i].GameResult.Message, out color);
        GetComponent<Image>().color = color;
    }
}
