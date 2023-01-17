namespace CloudNative.CloudEvents.Endpoints
{

    /// <summary>
    /// A credential consisting of a set of headers.
    /// </summary>
    public interface IHeaderEndpointCredential : IEndpointCredential
    {
        /// <summary>
        /// The headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }
    }
}