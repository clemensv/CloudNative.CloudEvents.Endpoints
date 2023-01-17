// (c) Cloud Native Computing Foundation. See LICENSE for details

using Microsoft.Extensions.Logging;

namespace CloudNative.CloudEvents.Endpoints
{

    /// <summary>
    /// Abstract base class for producer endpoints.
    /// </summary>
    abstract public class ProducerEndpoint
    {
        /// <summary>
        /// Delegate for a hook to allow a custom producer endpoint to be created.
        /// </summary>
        public delegate ProducerEndpoint ProducerEndpointFactoryHandler(IEndpointCredential credential, Protocol protocol, Dictionary<string, string> options, List<Uri> endpoints);
        private static ProducerEndpointFactoryHandler? _producerEndpointFactoryHook;
        
        /// <summary>
        /// Hook to allow a custom producer endpoint to be created.
        /// </summary>
        /// <remarks>
        /// This hook is intended to allow a custom producer endpoint to be created. It is not intended to be used to modify the behaviour of an existing endpoint.
        /// </remarks>
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
                
        /// <summary>
        /// Creates a producer endpoint for the specified protocol and with the specified options and endpoints.
        /// </summary>
        /// <param name="logger">The logger to use when creating the endpoint.</param>
        /// <param name="credential">The credential to use when creating the endpoint.</param>
        /// <param name="protocol">The protocol to use when creating the endpoint.</param>
        /// <param name="options">The options to use when creating the endpoint.</param>
        /// <param name="endpoints">The endpoints to use when creating the endpoint.</param>
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

        /// <summary>
        /// Sends a CloudEvent to the endpoint.
        /// </summary>
        /// <param name="cloudEvent">The CloudEvent to send.</param>
        /// <param name="contentMode">The content mode to use when sending the event.</param>
        /// <param name="formatter">The formatter to use when sending the event.</param>
        public abstract Task SendAsync(CloudEvent cloudEvent, ContentMode contentMode, CloudEventFormatter formatter);
    }
}