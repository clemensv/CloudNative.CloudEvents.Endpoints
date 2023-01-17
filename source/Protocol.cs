// (c) Cloud Native Computing Foundation. See LICENSE for details

namespace CloudNative.CloudEvents.Endpoints
{
    /// <summary>
    /// The protocol used to send a CloudEvent.
    /// </summary>
    public enum Protocol { 
        Http, 
        Amqp, 
        Mqtt 
    }
}