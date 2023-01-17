// (c) Cloud Native Computing Foundation. See LICENSE for details

namespace CloudNative.CloudEvents.Endpoints
{
    /// <summary>
    /// A plain endpoint credential that uses a client ID and client secret.
    /// </summary>
    public class PlainEndpointCredential : IPlainEndpointCredential
    {
        private readonly string clientId;
        private readonly string clientSecret;

        /// <summary>
        /// Creates a new plain endpoint credential.
        /// </summary>
        /// <param name="clientId">The client ID.</param>
        /// <param name="clientSecret">The client secret.</param>
        public PlainEndpointCredential(string clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        }

        /// <summary>
        /// The client ID.
        /// </summary>
        public string ClientId => clientId;
        /// <summary>
        /// The client secret.
        /// </summary>
        public string ClientSecret => clientSecret;
    }
}