namespace CloudNative.CloudEvents.Experimental.Endpoints
{
    /// <summary>
    /// A credential consisting of a client ID and client secret.
    /// </summary>
    public interface IPlainEndpointCredential : IEndpointCredential
    {
        /// <summary>
        /// The client ID.
        /// </summary>
        public string ClientId { get; }
        /// <summary>
        /// The client secret.
        /// </summary>
        public string ClientSecret { get; }
    }
}