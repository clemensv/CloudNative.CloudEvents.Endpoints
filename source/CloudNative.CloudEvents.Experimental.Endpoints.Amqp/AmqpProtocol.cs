using CloudNative.CloudEvents.Experimental.Endpoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudNative.CloudEvents.Experimental.Endpoints
{
    public static class AmqpProtocol
    {
        public const string Name = "amqp";

        static AmqpProtocol()
        {
            AmqpConsumerEndpoint.Register();
            AmqpProducerEndpoint.Register();
        }
    }
}
