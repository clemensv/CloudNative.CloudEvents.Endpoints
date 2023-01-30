// (c) Cloud Native Computing Foundation. See LICENSE for details

namespace CloudNative.CloudEvents.Experimental.Endpoints
{
    /// <summary>
    /// A credential for a token endpoint.
    /// </summary>
    public interface ITokenEndpointCredential : IEndpointCredential
    {
        /// <summary>
        /// The token endpoint.
        /// </summary>
        public Task<string> GetTokenAsync();
    }
}