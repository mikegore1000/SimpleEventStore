using System.Threading.Tasks;
using NUnit.Framework;
using SimpleEventStore.InMemory;

namespace SimpleEventStore.Tests.InMemory
{
    [TestFixture]
    public class InMemoryEventStoreReading : EventStoreReading
    {
        protected override Task<IStorageEngine> CreateStorageEngine()
        {
            return Task.FromResult((IStorageEngine)new InMemoryStorageEngine());
        }
    }
}