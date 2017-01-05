using System.Threading.Tasks;

namespace SimpleEventStore.Tests
{
    public abstract class EventStoreTestBase
    {
        private EventStore eventStore;

        protected async Task<EventStore> GetEventStore()
        {
            if (this.eventStore == null)
            {
                var storageEngine = await CreateStorageEngine();
                this.eventStore = new EventStore(storageEngine);
            }

            return this.eventStore;
        }

        protected abstract Task<IStorageEngine> CreateStorageEngine();
    }
}