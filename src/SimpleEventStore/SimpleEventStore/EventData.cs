using System;

namespace SimpleEventStore
{
    public class EventData
    {
        public Guid EventId { get; private set; }

        public object Body { get; private set; }

        public object Metadata { get; private set; }

        public EventData(Guid eventId, object body)
        {
            Guard.IsNotNull(nameof(body), body);

            EventId = eventId;
            Body = body;
        }

        public EventData(Guid eventId, object body, object metadata) : this(eventId, body)
        {
            Metadata = metadata;
        }
    }
}