namespace SimpleEventStore
{
    public class StorageEvent
    {
        public string StreamId { get; private set; }

        public object EventBody { get; private set; }

        public StorageEvent(string streamId, object eventBody)
        {
            StreamId = streamId;
            EventBody = eventBody;
        }
    }
}
