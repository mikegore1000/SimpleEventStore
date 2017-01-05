using SimpleEventStore.InMemory;

namespace SimpleEventStore.Tests.InMemory
{
    public class InMemoryEventStoreCatchUpSubscription : EventStoreCatchUpSubscription
    {
        protected override IStorageEngine CreateStorageEngine()
        {
            return new InMemoryStorageEngine();
        }
    }
}
