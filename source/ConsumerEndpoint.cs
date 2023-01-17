using System;
using System.Collections.Generic;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Extensions;
using Microsoft.Extensions.Logging;

namespace CloudNative.CloudEvents.Endpoints
{
    abstract class ConsumerEndpoint
    {
        protected Dictionary<Type, Action<CloudEvent, object>> EventHandlers { get; set; }

        public ConsumerEndpoint()
        {
            EventHandlers = new Dictionary<Type, Action<CloudEvent, object>>();
        }

        public void RegisterEventHandler<T>(Action<CloudEvent, T> handler)
        {
            EventHandlers[typeof(T)] = (e, d) => handler(e, (T)d);
        }

        public abstract Task StartAsync();
        public abstract Task StopAsync();

        protected void DeliverEvent(CloudEvent cloudEvent, object? data)
        {
            if (EventHandlers.TryGetValue(data?.GetType(), out var handler))
            {
                handler(cloudEvent, data);
            }
        }

        public delegate ConsumerEndpoint ConsumerEndpointFactoryHandler(IEndpointCredential credential, Protocol protocol, Dictionary<string, string> options, List<Uri> endpoints);
        private static ConsumerEndpointFactoryHandler? _ConsumerEndpointFactoryHook;
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