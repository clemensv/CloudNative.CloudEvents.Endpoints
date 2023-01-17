using System;
using System.Collections.Generic;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Extensions;
using Microsoft.Extensions.Logging;

namespace CloudNative.CloudEvents.Endpoints
{
    /// <summary>
    /// Base class for consumer endpoints.
    /// </summary>
    public abstract class ConsumerEndpoint
    {
        public event Func<CloudEvent, ILogger, Task>? DispatchEventAsync;

        /// <summary>
        /// Creates a new consumer endpoint.
        /// </summary
        public ConsumerEndpoint(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Starts the endpoint.
        /// </summary>
        public abstract Task StartAsync();
        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        public abstract Task StopAsync();

        /// <summary>
        /// Delivers the specified event to the registered event handlers.
        /// </summary>
        /// <param name="cloudEvent">The event to deliver.</param>
        /// <param name="data">The data associated with the event.</param>

        protected void DeliverEvent(CloudEvent cloudEvent)
        {
            
            if ( DispatchEventAsync != null )
            {
                DispatchEventAsync.Invoke(cloudEvent, Logger);
            }
        }

        public delegate ConsumerEndpoint ConsumerEndpointFactoryHandler(ILogger logger, IEndpointCredential credential, Protocol protocol, Dictionary<string, string> options, List<Uri> endpoints);
        private static ConsumerEndpointFactoryHandler? _ConsumerEndpointFactoryHook;

        public ILogger Logger { get; }

        /// <summary>
        /// Hook to allow a custom consumer endpoint to be created.
        /// </summary>
        /// <remarks>
        /// This hook is intended to allow a custom consumer endpoint to be created. It is not intended to be used to modify the behaviour of an existing endpoint.
        /// </remarks>
        public event ConsumerEndpointFactoryHandler ConsumerEndpointFactoryHook
        {
            add
            {
                if (_ConsumerEndpointFactoryHook == null)
                {
                    _ConsumerEndpointFactoryHook = value;
                }
                else
                {
                    throw new InvalidOperationException("ConsumerEndpointFactoryHook already set");

                }
            }
            remove
            {
                _ConsumerEndpointFactoryHook = null;
            }
        }

        /// <summary>
        /// Creates a consumer endpoint for the specified protocol and with the specified options and endpoints.
        /// </summary>
        /// <param name="logger">The logger to use when creating the endpoint.</param>
        /// <param name="credential">The credential to use when creating the endpoint.</param>
        /// <param name="protocol">The protocol to use when creating the endpoint.</param>
        /// <param name="options">The options to use when creating the endpoint.</param>
        /// <param name="endpoints">The endpoints to use when creating the endpoint.</param>
        /// <param name="deserializeCloudEventData">The function to use to deserialize the event data.</param>
        public static ConsumerEndpoint Create(ILogger logger, IEndpointCredential credential, Protocol protocol, Dictionary<string, string> options, List<Uri> endpoints)
        {
            ConsumerEndpoint? ep = _ConsumerEndpointFactoryHook?.Invoke(logger, credential, protocol, options, endpoints);
            if (ep != null)
            {
                return ep;
            }

            switch (protocol)
            {
                case Protocol.Amqp:
                    return new AmqpConsumerEndpoint(logger, credential, options, endpoints);
                case Protocol.Mqtt:
                    return new MqttConsumerEndpoint(logger, credential, options, endpoints);
                default:
                    throw new NotSupportedException($"Protocol '{protocol}' is not supported.");
            }
        }
    }
}