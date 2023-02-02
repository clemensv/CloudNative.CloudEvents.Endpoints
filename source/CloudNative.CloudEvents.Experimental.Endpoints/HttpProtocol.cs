namespace CloudNative.CloudEvents.Experimental.Endpoints
{
    public static class HttpProtocol
    {
        public const string Name = "http";

        public static void Initialize()
        {
            HttpListenerConsumerEndpoint.Register();
        }
    }
}
