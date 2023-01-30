using CloudNative.CloudEvents.Experimental.Endpoints;

namespace CloudNative.CloudEvents.Experimental.Endpoints
{
    public static class MqttProtocol
    {
        public const string Name = "mqtt";

        static MqttProtocol()
        {
            MqttConsumerEndpoint.Register();
            MqttProducerEndpoint.Register();
        }
    }
}
