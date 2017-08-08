using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace Wake
{
    [TestFixture]
    public class WakeNetTests
    {
        [Test]
        public IEnumerator CreateServer()
        {
            WakeNet.Init(WakeNetConfig.Default);

            var server = WakeNet.CreateServer(10, 9999);

            Assert.NotNull(server);
            Assert.AreEqual(1, WakeNet.Servers.Count);
            Assert.AreEqual(server, WakeNet.Servers[0]);
            Assert.IsTrue(server.IsConnected);

            WakeNet.DestroyServer(server);

            yield return new WaitForSeconds(1);

            Assert.NotNull(server);
            Assert.AreEqual(0, WakeNet.Servers.Count);
            Assert.IsFalse(server.IsConnected);
            yield return new WaitForSeconds(1);

        }

        [Test]
        public IEnumerator CreateClient()
        {
            WakeNet.Init(WakeNetConfig.Default);

            var client = WakeNet.CreateClient();

            Assert.NotNull(client);
            Assert.AreEqual(1, WakeNet.Clients.Count);
            Assert.AreEqual(client, WakeNet.Clients[0]);
            Assert.IsFalse(client.IsConnected);

            WakeNet.DestroyClient(client);

            yield return new WaitForSeconds(1);

            Assert.NotNull(client);
            Assert.AreEqual(0, WakeNet.Clients.Count);
            Assert.IsFalse(client.IsConnected);

            yield return new WaitForSeconds(1);
        }
    }
}
