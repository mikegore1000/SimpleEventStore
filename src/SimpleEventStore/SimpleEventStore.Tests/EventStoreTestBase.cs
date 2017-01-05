namespace SimpleEventStore.Tests
{
    public abstract class EventStoreTestBase
    {
        protected EventStore GetEventStore()
        {
            var storageEngine = CreateStorageEngine();
            return new EventStore(storageEngine);
        }

        protected abstract IStorageEngine CreateStorageEngine();
    }
}