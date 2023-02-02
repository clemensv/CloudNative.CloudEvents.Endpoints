using CloudNative.CloudEvents.Experimental.Endpoints;

namespace CloudNative.CloudEvents.Experimental.Endpoints
{
    public static class MqttProtocol
    {
        public const string Name = "mqtt";
        public static void Initialize()
        {
            MqttConsumerEndpoint.Register();
            MqttProducerEndpoint.Register();
        }
    }
}
