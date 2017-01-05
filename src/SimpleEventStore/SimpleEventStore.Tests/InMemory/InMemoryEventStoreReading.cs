using SimpleEventStore.InMemory;

namespace SimpleEventStore.Tests.InMemory
{
    public class InMemoryEventStoreReading : EventStoreReading
    {
        protected override IStorageEngine CreateStorageEngine()
        {
            return new InMemoryStorageEngine();
        }
    }
}