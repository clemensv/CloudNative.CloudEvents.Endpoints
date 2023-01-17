// (c) Cloud Native Computing Foundation. See LICENSE for details

using CloudNative.CloudEvents.Http;
using Microsoft.Extensions.Logging;

namespace CloudNative.CloudEvents.Endpoints
{
    class HttpProducerEndpoint : ProducerEndpoint
    {
        private HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IEndpointCredential _credential;
        private List<Uri> _endpoints;

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