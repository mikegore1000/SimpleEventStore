using System;

namespace SimpleEventStore
{
    public class EventData
    {
        public object Body { get; private set; }

        public object Metadata { get; private set; }

        public EventData(object body)
        {
            Guard.IsNotNull(nameof(body), body);
            Body = body;
        }

        public EventData(object body, object metadata) : this(body)
        {
            Guard.IsNotNull(nameof(metadata), metadata);
            Metadata = metadata;
        }
    }
}