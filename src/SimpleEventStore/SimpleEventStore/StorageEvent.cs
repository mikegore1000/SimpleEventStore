namespace SimpleEventStore
{
    public class StorageEvent
    {
        public string StreamId { get; private set; }

        public object EventBody { get; private set; }

        public int EventNumber { get; private set; }

        public StorageEvent(string streamId, object eventBody, int eventNumber)
        {
            StreamId = streamId;
            EventBody = eventBody;
            EventNumber = eventNumber;
        }
    }
}
