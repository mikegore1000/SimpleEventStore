namespace SimpleEventStore.Tests.InMemory
{
    public class InMemoryEventStoreAppending : EventStoreAppending
    {
        protected override IStorageEngine CreateStorageEngine()
        {
            return new InMemoryStorageEngine();
        }
    }
}