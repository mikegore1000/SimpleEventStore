using System.Threading.Tasks;
using SimpleEventStore.InMemory;

namespace SimpleEventStore.Tests.InMemory
{
    public class InMemoryEventStoreAllStreamReading : EventStoreAllStreamReading
    {
        protected override Task<IStorageEngine> CreateStorageEngine()
        {
            return Task.FromResult((IStorageEngine)new InMemoryStorageEngine());
        }
    }
}