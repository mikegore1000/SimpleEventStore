namespace SimpleEventStore.Tests.InMemory
{
    public class InMemoryEventStoreAllStreamReading : EventStoreAllStreamReading
    {
        protected override IStorageEngine CreateStorageEngine()
        {
            return new InMemoryStorageEngine();
        }
    }
}