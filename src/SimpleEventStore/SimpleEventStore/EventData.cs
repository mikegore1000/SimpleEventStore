namespace SimpleEventStore
{
    public class EventData
    {
        public object Body { get; private set; }

        public object Metadata { get; private set; }

        public EventData(object body)
        {
            Body = body;
        }

        public EventData(object body, object metadata)
        {
            Body = body;
            Metadata = metadata;
        }
    }
}