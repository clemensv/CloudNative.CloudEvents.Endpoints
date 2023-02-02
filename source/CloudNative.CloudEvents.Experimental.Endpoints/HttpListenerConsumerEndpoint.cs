﻿using System.Net;
using System.Net.Mime;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Http;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Logging;
using CloudNative.CloudEvents.Protobuf;


namespace CloudNative.CloudEvents.Experimental.Endpoints
{
    /// <summary>
    /// A consumer endpoint that listeners for CloudEvents via HTTP
    /// </summary>
    public class HttpListenerConsumerEndpoint : ConsumerEndpoint
    {
        private const string ERROR_LOG_TEMPLATE = "Error in HttpListenerConsumerEndpoint: {0}";
        private const string VERBOSE_LOG_TEMPLATE = "HttpListenerConsumerEndpoint: {0}";

        private CloudEventFormatter _jsonFormatter = new JsonEventFormatter();
        private CloudEventFormatter _protoFormatter = new ProtobufEventFormatter();
        private CloudEventFormatter _avroFormatter = new global::CloudNative.CloudEvents.Avro.AvroEventFormatter();
        private IEndpointCredential _credential;
        private List<Uri> _endpoints;
        private HttpListener? _listener;
        private Task? _listenerTask;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// Creates a new HttpListener consumer endpoint.
        /// </summary>
        /// <param name="logger">The logger to use for this endpoint.</param>
        /// <param name="credential">The credential to use for this endpoint.</param>
        /// <param name="options">The options to use for this endpoint.</param>
        /// <param name="endpoints">The endpoints to use for this endpoint.</param>
        /// <param name="deserializeCloudEventData">The function to use to deserialize the CloudEvent data.</param>
        internal HttpListenerConsumerEndpoint(ILogger logger, IEndpointCredential credential, Dictionary<string, string> options, List<Uri> endpoints) : base(logger)
        {
            _credential = credential;
            _endpoints = endpoints;
        }

        /// <summary>
        /// Starts the endpoint.
        /// </summary>
        /// <returns>A task that completes when the endpoint has started.</returns>
        public override Task StartAsync()
        {
            Uri endpoint = _endpoints.First();

            try
            {
                _listener = new HttpListener();
                foreach (var ep in _endpoints)
                {
                    if (ep.Scheme.StartsWith("http"))
                    {
                        _listener.Prefixes.Add(ep.AbsoluteUri);
                    }
                }
                _listener.Start();
                _listenerTask = Task.Run(async ()=>await Listener(_listener, _cts.Token));
                
                Logger.LogInformation(VERBOSE_LOG_TEMPLATE, "Listener started");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ERROR_LOG_TEMPLATE, "Error creating listnener");
                throw;
            }
            return Task.CompletedTask;
        }

        async Task Listener(HttpListener listener, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var ctx = await listener.GetContextAsync();
                    if (ctx.Request.IsCloudEvent())
                    {
                        try
                        {
                            OnMessage(ctx.Request);
                            ctx.Response.StatusCode = 204;
                        }
                        catch( Exception ex1)
                        {
                            Logger.LogError(ex1, ERROR_LOG_TEMPLATE, "Error processing message");
                            ctx.Response.StatusCode = 500;
                        }
                    }
                    else
                    {
                        ctx.Response.StatusCode = 400;
                    }
                    ctx.Response.Close();
                }
            }
            catch(HttpListenerException lex)
            {
                if ( lex.ErrorCode == 995 )
                {
                    // thread exit
                    return;
                }
                Logger.LogError(lex, ERROR_LOG_TEMPLATE, "Error in listener");
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, ERROR_LOG_TEMPLATE, "Error in listener");
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (_listener != null)
            {
                _listener.Stop();
                _listener.Close();
            }
        }

        protected override void Deliver<T>(T message)
        {
            base.Deliver(message);
        }

        /// <summary>
        /// Called when a message is received.
        /// </summary>
        /// <param name="request">The message.</param>
        private void OnMessage(HttpListenerRequest request)
        {
            try
            {
                if (request.IsCloudEvent())
                {
                    CloudEventFormatter formatter;
                    var contentType = request.ContentType?.ToString().Split(';')[0];
                    if (contentType != null && contentType.EndsWith("+proto"))
                    {
                        formatter = _protoFormatter;
                    }
                    else if (contentType != null && contentType.EndsWith("+avro"))
                    {
                        formatter = _avroFormatter;
                    }
                    else
                    {
                        formatter = _jsonFormatter;
                    }
                    var cloudEvent = request.ToCloudEvent(formatter);
                    Deliver(cloudEvent);
                }
                else
                {
                    Deliver(request);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ERROR_LOG_TEMPLATE, "Error processing message: " + ex.Message);
            }
        }

        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        public override async Task StopAsync()
        {
            Logger.LogInformation(VERBOSE_LOG_TEMPLATE, "Stopping HttpListener consumer endpoint");
            _cts.Cancel();
            if (_listener != null && _listenerTask != null)
            {
                _listener.Stop();
                await _listenerTask;
            }            
        }


        internal static void Register()
        {
            AddConsumerEndpointFactoryHook(CreateHttpListener);
        }

        private static ConsumerEndpoint? CreateHttpListener(ILogger logger, IEndpointCredential credential, string protocol, Dictionary<string, string> options, List<Uri> endpoints)
        {
            switch (protocol)
            {
                case "http":
                    return new HttpListenerConsumerEndpoint(logger, credential, options, endpoints);
            }
            return null;
        }
    }

}