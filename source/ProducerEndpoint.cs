// (c) Cloud Native Computing Foundation. See LICENSE for details

using Microsoft.Extensions.Logging;

namespace CloudNative.CloudEvents.Endpoints
{

    abstract public class ProducerEndpoint
    {
        public delegate ProducerEndpoint ProducerEndpointFactoryHandler(IEndpointCredential credential, Protocol protocol, Dictionary<string, string> options, List<Uri> endpoints);
        private static ProducerEndpointFactoryHandler? _producerEndpointFactoryHook;
        public event ProducerEndpointFactoryHandler ProducerEndpointFactoryHook
        {
            add
            {
                if (_producerEndpointFactoryHook == null)
                {
                    _producerEndpointFactoryHook = value;
                }
                else
                {
                    throw new InvalidOperationException("ProducerEndpointFactoryHook already set");

                }
            }
            remove
            {
                _producerEndpointFactoryHook = null;
            }
        }
                
        public static ProducerEndpoint Create(ILogger logger, IEndpointCredential credential, Protocol protocol, Dictionary<string, string> options, List<Uri> endpoints)
        {
            ProducerEndpoint? ep = _producerEndpointFactoryHook?.Invoke(credential, protocol, options, endpoints);
            if (ep != null)
            {
                return ep;
            }
            
            switch (protocol)
            {
                case Protocol.Http:
                    return new HttpProducerEndpoint(logger, credential, options, endpoints);
                case Protocol.Amqp:
                    return new AmqpProducerEndpoint(logger, credential, options, endpoints);
                case Protocol.Mqtt:
                    return new MqttProducerEndpoint(logger, credential, options, endpoints);
                default:
                    throw new NotSupportedException($"Protocol '{protocol}' is not supported.");
            }
        }

        public abstract Task SendAsync(CloudEvent cloudEvent, ContentMode contentMode, CloudEventFormatter formatter);
    }
}