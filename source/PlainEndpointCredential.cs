// (c) Cloud Native Computing Foundation. See LICENSE for details

namespace CloudNative.CloudEvents.Endpoints
{
    public class PlainEndpointCredential : IPlainEndpointCredential
    {
        private readonly string clientId;
        private readonly string clientSecret;

        public PlainEndpointCredential(string clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        }
        public string ClientId => clientId;
        public string ClientSecret => clientSecret;
    }
}