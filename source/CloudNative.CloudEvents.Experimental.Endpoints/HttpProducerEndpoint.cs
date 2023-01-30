// (c) Cloud Native Computing Foundation. See LICENSE for details

using CloudNative.CloudEvents.Http;
using Microsoft.Extensions.Logging;

namespace CloudNative.CloudEvents.Experimental.Endpoints
{

    /// <summary>
    /// Producer endpoint for HTTP.
    /// </summary>
    class HttpProducerEndpoint : ProducerEndpoint
    {
        private HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IEndpointCredential _credential;
        private List<Uri> _endpoints;

        /// <summary>
        /// Creates a new producer endpoint for HTTP.
        /// </summary>
        /// <param name="logger">The logger to use when creating the endpoint.</param>
        /// <param name="credential">The credential to use when creating the endpoint.</param>
        /// <param name="options">The options to use when creating the endpoint.</param>
        /// <param name="endpoints">The endpoints to use when creating the endpoint.</param>
        public HttpProducerEndpoint(ILogger logger, IEndpointCredential credential, Dictionary<string, string> options, List<Uri> endpoints)
        {
            this._logger = logger;
            this._credential = credential;
            this._endpoints = endpoints;
            _httpClient = new HttpClient();
            if ( credential is IHeaderEndpointCredential )
            {
                foreach(var header in ((IHeaderEndpointCredential)credential).Headers)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
        }

        /// <summary>
        /// Sends a CloudEvent to the endpoint.
        /// </summary>
        /// <param name="cloudEvent">The CloudEvent to send.</param>
        /// <param name="contentMode">The content mode to use when sending the CloudEvent.</param>
        /// <param name="formatter">The formatter to use when sending the CloudEvent.</param>
        public override async Task SendAsync(CloudEvent cloudEvent, ContentMode contentMode, CloudEventFormatter formatter)
        {
            try
            {
                foreach (var endpoint in _endpoints)
                {
                    try
                    {
                        await _httpClient.PostAsync(endpoint, cloudEvent.ToHttpContent(contentMode, formatter));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending message to endpoint {endpoint}", endpoint);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to endpoints");
            }           
            
        }

    }
}