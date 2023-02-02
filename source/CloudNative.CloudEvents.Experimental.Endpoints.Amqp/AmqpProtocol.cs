using CloudNative.CloudEvents.Experimental.Endpoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudNative.CloudEvents.Experimental.Endpoints
{
    public static class AmqpProtocol
    {
        public const string Name = "amqp";

        public static void Initialize()
        {
            AmqpConsumerEndpoint.Register();
            AmqpProducerEndpoint.Register();
        }
    }
}
