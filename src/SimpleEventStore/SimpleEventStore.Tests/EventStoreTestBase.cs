namespace SimpleEventStore.Tests
{
    public abstract class EventStoreTestBase
    {
        protected EventStore CreateEventStore()
        {
            return new EventStore(CreateStorageEngine());
        }

        protected abstract IStorageEngine CreateStorageEngine();
    }
}