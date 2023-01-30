using System;
using System.Collections.Generic;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Extensions;
using Microsoft.Extensions.Logging;

namespace CloudNative.CloudEvents.Experimental.Endpoints
{
    /// <summary>
    /// Base class for consumer endpoints.
    /// </summary>
    public abstract class ConsumerEndpoint : IDisposable
    {
        public event Func<CloudEvent, ILogger, Task>? DispatchCloudEventAsync;

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

        protected virtual void Deliver<T>(T message) where T : class
        {
            if (message is CloudEvent && DispatchCloudEventAsync != null )
            {
                DispatchCloudEventAsync.Invoke((CloudEvent)(object)message, Logger);
            }
        }
        
        public delegate ConsumerEndpoint? ConsumerEndpointFactoryHandler(ILogger logger, IEndpointCredential credential, string protocol, Dictionary<string, string> options, List<Uri> endpoints);
        private static List<ConsumerEndpointFactoryHandler> _consumerEndpointFactoryHooks = new List<ConsumerEndpointFactoryHandler>();
        
        public ILogger Logger { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hook"></param>
        public static void AddConsumerEndpointFactoryHook(ConsumerEndpointFactoryHandler hook)
        {
            _consumerEndpointFactoryHooks.Add(hook);
        }

        public static void RemoveConsumerEndpointFactoryHook(ConsumerEndpointFactoryHandler hook)
        {
            _consumerEndpointFactoryHooks.Remove(hook);
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
        public static ConsumerEndpoint Create(ILogger logger, IEndpointCredential credential, string protocol, Dictionary<string, string> options, List<Uri> endpoints)
        {
            foreach (var hook in _consumerEndpointFactoryHooks)
            {
                ConsumerEndpoint? ep = hook.Invoke(logger, credential, protocol, options, endpoints);
                if (ep != null)
                {
                    return ep;
                }
            }

            switch (protocol)
            {
                default:
                    throw new NotSupportedException($"Protocol '{protocol}' is not supported.");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~ConsumerEndpoint()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}