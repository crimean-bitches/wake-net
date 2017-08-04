#region Usings

using System.Collections;
using UnityEngine;
using WakeNet.Internal;
using WakeNet.Protocol.Proxy.Messages;

#endregion

namespace WakeNet
{
    public class Test : MonoBehaviour
    {
        // Use this for initialization
        private IEnumerator Start()
        {
            NetManager.Init();

            var server = new Server(11301);
            server.Start();
            var client = new Client();

            server.ClientConnected += ClientConnected;
            
            client.Connect("127.0.0.1", 11301);
            yield return new WaitForSeconds(1);

            var proxy1 = client.AddProxy<EmptyMessage, EmptyMessage>(1, 0);
            var proxy2 = client.AddProxy<StringMessage, StringMessage>(2, 0);

            proxy1.Send(new EmptyMessage());
            proxy2.Send(new StringMessage("HELLO"));

            yield return new WaitForSeconds(1);
            
            yield return new WaitForSeconds(1);

            //client.Disconnect();
        }

        private void ClientConnected(Client client)
        {
            var proxy = client.AddProxy<EmptyMessage, EmptyMessage>(1, 0);
            var proxy2 = client.AddProxy<StringMessage, StringMessage>(2, 0);
            proxy.Received += message =>
            {
                Debug.Log("YAY WORKS");
            };
            proxy2.Received += message =>
            {
                Debug.Log("YAY WORKS again : " + message.Data);
            };
        }
    }
}