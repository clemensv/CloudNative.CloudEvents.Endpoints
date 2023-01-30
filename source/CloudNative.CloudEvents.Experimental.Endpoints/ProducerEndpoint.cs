// (c) Cloud Native Computing Foundation. See LICENSE for details

using Microsoft.Extensions.Logging;

namespace CloudNative.CloudEvents.Experimental.Endpoints
{

    /// <summary>
    /// Abstract base class for producer endpoints.
    /// </summary>
    abstract public class ProducerEndpoint
    {
        /// <summary>
        /// Delegate for a hook to allow a custom producer endpoint to be created.
        /// </summary>
        public delegate ProducerEndpoint? ProducerEndpointFactoryHandler(ILogger logger, IEndpointCredential credential, string protocol, Dictionary<string, string> options, List<Uri> endpoints);
        private static List<ProducerEndpointFactoryHandler> _producerEndpointFactoryHooks = new List<ProducerEndpointFactoryHandler>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        public static void AddProducerEndpointFactoryHandler(ProducerEndpointFactoryHandler handler)
        {
            _producerEndpointFactoryHooks.Add(handler);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        public static void RemoveProducerEndpointFactoryHandler(ProducerEndpointFactoryHandler handler)
        {
            _producerEndpointFactoryHooks.Remove(handler);
        }


        /// <summary>
        /// Creates a producer endpoint for the specified protocol and with the specified options and endpoints.
        /// </summary>
        /// <param name="logger">The logger to use when creating the endpoint.</param>
        /// <param name="credential">The credential to use when creating the endpoint.</param>
        /// <param name="protocol">The protocol to use when creating the endpoint.</param>
        /// <param name="options">The options to use when creating the endpoint.</param>
        /// <param name="endpoints">The endpoints to use when creating the endpoint.</param>
        public static ProducerEndpoint Create(ILogger logger, IEndpointCredential credential, string protocol, Dictionary<string, string> options, List<Uri> endpoints)
        {
            foreach (var handler in _producerEndpointFactoryHooks)
            {
                ProducerEndpoint? ep = handler.Invoke(logger, credential, protocol, options, endpoints);
                if (ep != null)
                {
                    return ep;
                }
            }
            
            switch (protocol)
            {
                case HttpProtocol.Name:
                    return new HttpProducerEndpoint(logger, credential, options, endpoints);
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