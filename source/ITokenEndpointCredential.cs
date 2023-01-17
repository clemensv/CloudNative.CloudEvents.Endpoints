// (c) Cloud Native Computing Foundation. See LICENSE for details

namespace CloudNative.CloudEvents.Endpoints
{
    public interface ITokenEndpointCredential : IEndpointCredential
    {
        public Task<string> GetTokenAsync();
    }
}