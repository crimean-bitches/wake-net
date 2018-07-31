namespace Wake
{
    public static class WakeError
    {
        public const string NotInitialized =
            "Calling methods on not initialized instance of WakeNet. Call WakeNet.Init() first.";

        public const string NotImplemented = "This thing is currently not implemented, keep eye on repository changes.";
        public const string InvalidHostCreated = "Host with invalid id were created by NetworkTransport.AddHost().";

        public const string ServerNotExists = "Server you wish to operate not exists or already has been destroyed.";
        public const string ClientNotExists = "Client you wish to operate not exists or already has been destroyed.";

        public const string DiscoveryNotExists =
            "Discovery you wish to operate not exists or already has been destroyed.";

        public const string ClientAlreadyExists = "Client already connected to server.";

        public const string NotConnected = "Client not connected and can not be disconnected.";
    }
}