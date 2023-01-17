namespace CloudNative.CloudEvents.Endpoints
{
    public interface IHeaderEndpointCredential : IEndpointCredential
    {
        public Dictionary<string, string> Headers { get; }
    }
}