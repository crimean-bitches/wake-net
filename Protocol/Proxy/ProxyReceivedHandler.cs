namespace Wake.Protocol.Proxy
{
    public delegate void ProxyReceivedHandler<TMessage>(TMessage message, int connectionId);
}