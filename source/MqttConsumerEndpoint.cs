
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using Microsoft.Extensions.Logging;
using CloudNative.CloudEvents.SystemTextJson;
using CloudNative.CloudEvents.Protobuf;
using CloudNative.CloudEvents.Mqtt;

using MQTTnet.Protocol;

namespace CloudNative.CloudEvents.Endpoints
{
    /// <summary>
    /// A consumer endpoint that receives CloudEvents from an MQTT broker.
    /// </summary>
    class MqttConsumerEndpoint : ConsumerEndpoint
    {
        private const string ERROR_LOG_TEMPLATE = "Error in MQTTConsumerEndpoint: {0}";
        private const string VERBOSE_LOG_TEMPLATE = "MQTTConsumerEndpoint: {0}";

        private IMqttClient? _client;
        private CloudEventFormatter _jsonFormatter = new JsonEventFormatter();
        private CloudEventFormatter _protoFormatter = new ProtobufEventFormatter();
        private CloudEventFormatter _avroFormatter = new global::CloudNative.CloudEvents.Avro.AvroEventFormatter();
        private readonly Func<CloudEvent, object>? _deserializeCloudEventData;
        private IEndpointCredential _credential;
        private IMqttClientOptions? _options;
        private ILogger _logger;
        private string _topic;
        private byte _qos;
        private List<Uri> _endpoints;

        /// <summary>
        /// Creates a new MQTT consumer endpoint.
        /// </summary>
        public MqttConsumerEndpoint(ILogger logger, IEndpointCredential credential, Dictionary<string, string> options, List<Uri> endpoints, Func<CloudEvent, object>? deserializeCloudEventData)
        {
            _logger = logger;
            _credential = credential;
            _endpoints = endpoints;
            _deserializeCloudEventData = deserializeCloudEventData;
            _client = new MqttFactory().CreateMqttClient();
            if ( options.TryGetValue("topic", out var topic))
            {
                _topic = topic;
            }
            if (options.TryGetValue("qos", out var qos))
            {
                _qos = 1;
                byte.TryParse(qos, out _qos);
            }
            if (_topic == null) throw new ArgumentException("topic is required");
        }
        
        /// <summary>
        /// Starts the endpoint.
        /// </summary>
        public override async Task StartAsync()
        {
            Uri endpoint = _endpoints.First();
            
            var optionsBuilder = new MqttClientOptionsBuilder();
            if (endpoint.Scheme == "mqtts")
            {
                optionsBuilder = optionsBuilder.WithTls();
            }
            optionsBuilder = optionsBuilder.WithTcpServer(endpoint.Host);
            if (_credential is IPlainEndpointCredential plainCredential)
            {
                optionsBuilder = optionsBuilder.WithCredentials(plainCredential.ClientId, plainCredential.ClientSecret);
            }
            else if (_credential is ITokenEndpointCredential tokenCredential)
            {
                var token = tokenCredential.GetTokenAsync().Result;
                optionsBuilder = optionsBuilder.WithCredentials("Bearer", System.Text.Encoding.ASCII.GetBytes(token));
            }
            _options = optionsBuilder.Build();
            await _client.ConnectAsync(_options);
            _client.UseApplicationMessageReceivedHandler(OnMessageReceived);
            await _client.SubscribeAsync(_topic, (MqttQualityOfServiceLevel)_qos);
            _logger.LogInformation(VERBOSE_LOG_TEMPLATE, "Started MQTT consumer endpoint");
        }

        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        public override async Task StopAsync()
        {
            await _client.DisconnectAsync();
            _logger.LogInformation(VERBOSE_LOG_TEMPLATE, "Stopped MQTT consumer endpoint");
        }
        
        /// <summary>
        /// Handles a received message.
        /// </summary>
        /// <param name="args">The message arguments.</param>
        private void OnMessageReceived(MqttApplicationMessageReceivedEventArgs args)
        {
            try
            {
                var cloudEvent = args.ApplicationMessage.ToCloudEvent(_jsonFormatter);
                object? data = cloudEvent.Data;
                if (_deserializeCloudEventData != null)
                {
                    data = _deserializeCloudEventData(cloudEvent);
                }
                DeliverEvent(cloudEvent, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ERROR_LOG_TEMPLATE, ex.Message);
            }
        }
    }
}