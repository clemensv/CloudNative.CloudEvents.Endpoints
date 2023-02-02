using Amqp;
using Amqp.Sasl;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Logging;
using CloudNative.CloudEvents.Protobuf;
using CloudNative.CloudEvents.Amqp;

namespace CloudNative.CloudEvents.Experimental.Endpoints
{
    /// <summary>
    /// A consumer endpoint that receives CloudEvents from an AMQP broker.
    /// </summary>
    public class AmqpConsumerEndpoint : ConsumerEndpoint
    {
        public event Func<Message, ILogger, Task>? DispatchMessageAsync;
        
        private const string ERROR_LOG_TEMPLATE = "Error in AMQPConsumerEndpoint: {0}";
        private const string VERBOSE_LOG_TEMPLATE = "AMQPConsumerEndpoint: {0}";

        private Connection? _connection;
        private Session? _session;
        private ReceiverLink? _receiverLink;
        private CloudEventFormatter _jsonFormatter = new JsonEventFormatter();
        private CloudEventFormatter _protoFormatter = new ProtobufEventFormatter();
        private CloudEventFormatter _avroFormatter = new global::CloudNative.CloudEvents.Avro.AvroEventFormatter();
        private string? _node;
        private IEndpointCredential _credential;
        private List<Uri> _endpoints;

        /// <summary>
        /// Creates a new AMQP consumer endpoint.
        /// </summary>
        /// <param name="logger">The logger to use for this endpoint.</param>
        /// <param name="credential">The credential to use for this endpoint.</param>
        /// <param name="options">The options to use for this endpoint.</param>
        /// <param name="endpoints">The endpoints to use for this endpoint.</param>
        /// <param name="deserializeCloudEventData">The function to use to deserialize the CloudEvent data.</param>
        internal AmqpConsumerEndpoint(ILogger logger, IEndpointCredential credential, Dictionary<string, string> options, List<Uri> endpoints) : base(logger)
        {
            _credential = credential;
            _endpoints = endpoints;
            if (options != null && options.TryGetValue("node", out var node))
            {
                _node = node;
            }
        }

        /// <summary>
        /// Starts the endpoint.
        /// </summary>
        /// <returns>A task that completes when the endpoint has started.</returns>
        public override async Task StartAsync()
        {
            Uri endpoint = _endpoints.First();
            var factory = new ConnectionFactory();

            Address address = new Address(
                endpoint.Host,
                endpoint.Port == -1 ? endpoint.Scheme == "amqps" ? 5671 : 5672 : endpoint.Port,
                path: _node != null ? _node : endpoint.AbsolutePath, scheme: endpoint.Scheme.ToUpper(),
                user: (_credential as IPlainEndpointCredential)?.ClientId,
                password: (_credential as IPlainEndpointCredential)?.ClientSecret);

            if (_credential is ITokenEndpointCredential tokenCredential)
            {
                factory.SASL.Profile = SaslProfile.Anonymous;
            }

            try
            {
                _connection = await factory.CreateAsync(address);
                Logger.LogInformation(VERBOSE_LOG_TEMPLATE, "Connection to endpoint " + endpoint + " created");
            }
            catch (Exception ex)
            {
                Logger.LogError(ERROR_LOG_TEMPLATE, "Error creating connection to endpoint " + endpoint + ": " + ex.Message);
                throw;
            }

            _session = new Session(_connection);
            if (_credential is ITokenEndpointCredential)
            {
                try
                {
                    var token = ((ITokenEndpointCredential)_credential).GetTokenAsync().Result;
                    var cbsSender = new SenderLink(_session, "$cbs", "$cbs");
                    var request = new global::Amqp.Message(token);
                    request.Properties.MessageId = Guid.NewGuid().ToString();
                    request.ApplicationProperties["operation"] = "put-token";
                    request.ApplicationProperties["type"] = "amqp:jwt";
                    request.ApplicationProperties["name"] = string.Format("amqp://{0}/{1}", address.Host, address.Path);
                    await cbsSender.SendAsync(request);
                    await cbsSender.CloseAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ERROR_LOG_TEMPLATE, "Error sending token to endpoint " + endpoint + ": " + ex.Message);
                    throw;
                }
            }
            Logger.LogInformation(VERBOSE_LOG_TEMPLATE, "Starting AMQP consumer endpoint");
            _receiverLink = new ReceiverLink(_session, "consumer-link", address.Path);
            _receiverLink.Start(10, OnMessage);
        }

        protected override void Dispose(bool disposing)
        {
            if (_receiverLink != null)
            {
                _receiverLink.Close();
                _receiverLink = null;
            }
            if (_session != null)
            {
                _session.Close();
                _session = null;
            }
            if (_connection != null)
            {
                _connection.Close();
                _connection = null;
            }
        }

        protected override void Deliver<T>(T message)
        {
            if (message is Message && DispatchMessageAsync != null)
            {
                DispatchMessageAsync.Invoke((Message)(object)message, Logger);
            }
            else
            {
                base.Deliver(message);
            }
        }

        /// <summary>
        /// Called when a message is received.
        /// </summary>
        /// <param name="receiver">The receiver link.</param>
        /// <param name="message">The message.</param>
        private void OnMessage(IReceiverLink receiver, global::Amqp.Message message)
        {
            try
            {
                if (message.IsCloudEvent())
                {
                    CloudEventFormatter formatter;
                    var contentType = message.Properties.ContentType?.ToString().Split(';')[0];
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
                    var cloudEvent = message.ToCloudEvent(formatter);
                    try
                    {

                        Deliver(cloudEvent);
                        receiver.Accept(message);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ERROR_LOG_TEMPLATE, "Error processing message: " + ex.Message);
                        receiver.Release(message);
                    }
                }
                else
                {
                    try
                    {
                        Deliver(message);
                        receiver.Accept(message);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ERROR_LOG_TEMPLATE, "Error processing message: " + ex.Message);
                        receiver.Release(message);
                    }
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
            Logger.LogInformation(VERBOSE_LOG_TEMPLATE, "Stopping AMQP consumer endpoint");
            if (_receiverLink != null)
                await _receiverLink.CloseAsync();
            if (_session != null)
                await _session.CloseAsync();
            if (_connection != null)
                await _connection.CloseAsync();
        }


        internal static void Register()
        {
            AddConsumerEndpointFactoryHook(CreateAmqp);
        }

        private static ConsumerEndpoint? CreateAmqp(ILogger logger, IEndpointCredential credential, string protocol, Dictionary<string, string> options, List<Uri> endpoints)
        {
            switch (protocol)
            {
                case "amqp":
                    return new AmqpConsumerEndpoint(logger, credential, options, endpoints);
            }
            return null;
        }
    }

}