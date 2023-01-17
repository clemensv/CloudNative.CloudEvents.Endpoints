// (c) Cloud Native Computing Foundation. See LICENSE for details

namespace CloudNative.CloudEvents.Endpoints
{
    /// <summary>
    /// The protocol used to send a CloudEvent.
    /// </summary>
    public enum Protocol
    {
        /// <summary>
        /// The HTTP protocol.
        /// </summary>
        Http,
        /// <summary>
        /// The AMQP protocol.
        /// </summary>
        Amqp,
        /// <summary>
        /// The MQTT protocol.
        /// </summary>
        Mqtt
    }
}