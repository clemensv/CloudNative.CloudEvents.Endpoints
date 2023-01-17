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
    abstract class ConsumerEndpoint
    {
        protected Dictionary<Type, Action<CloudEvent, object?>> EventHandlers { get; set; }

        /// <summary>
        /// Creates a new consumer endpoint.
        /// </summary
        public ConsumerEndpoint()
        {
            EventHandlers = new Dictionary<Type, Action<CloudEvent, object?>>();
        }

        /// <summary>
        /// Registers an event handler for the specified type of event.
        /// </summary>
        /// <typeparam name="T">The type of event to handle.</typeparam>
        /// <param name="handler">The event handler.</param>    
        public void RegisterEventHandler<T>(Action<CloudEvent, T?> handler) where T : class
        {
            EventHandlers[typeof(T)] = (CloudEvent e, object? d) => handler(e, d as T);
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

        protected void DeliverEvent(CloudEvent cloudEvent, object? data)
        {
            Type type = (data != null) ? data.GetType() : typeof(object);
            if (EventHandlers.TryGetValue(type, out var handler))
            {
                handler(cloudEvent, data);
            }
        }

        public delegate ConsumerEndpoint ConsumerEndpointFactoryHandler(IEndpointCredential credential, Protocol protocol, Dictionary<string, string> options, List<Uri> endpoints);
        private static ConsumerEndpointFactoryHandler? _ConsumerEndpointFactoryHook;
        
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
        public static ConsumerEndpoint Create(ILogger logger, IEndpointCredential credential, Protocol protocol, Dictionary<string, string> options, List<Uri> endpoints, Func<CloudEvent, object>? deserializeCloudEventData = null)
        {
            ConsumerEndpoint? ep = _ConsumerEndpointFactoryHook?.Invoke(credential, protocol, options, endpoints);
            if (ep != null)
            {
                return ep;
            }

            switch (protocol)
            {
                case Protocol.Amqp:
                    return new AmqpConsumerEndpoint(logger, credential, options, endpoints, deserializeCloudEventData);
                case Protocol.Mqtt:
                    return new MqttConsumerEndpoint(logger, credential, options, endpoints, deserializeCloudEventData);
                default:
                    throw new NotSupportedException($"Protocol '{protocol}' is not supported.");
            }
        }
    }
}