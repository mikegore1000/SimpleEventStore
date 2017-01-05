using System.Threading.Tasks;

namespace SimpleEventStore.Tests
{
    public abstract class EventStoreTestBase
    {
        protected async Task<EventStore> GetEventStore()
        {
            var storageEngine = await CreateStorageEngine();
            return new EventStore(storageEngine);
        }

        protected abstract Task<IStorageEngine> CreateStorageEngine();
    }
}