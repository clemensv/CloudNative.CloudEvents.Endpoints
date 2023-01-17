namespace CloudNative.CloudEvents.Endpoints
{
    public interface IPlainEndpointCredential : IEndpointCredential
    {
        public string ClientId { get; }
        public string ClientSecret { get; }
    }
}