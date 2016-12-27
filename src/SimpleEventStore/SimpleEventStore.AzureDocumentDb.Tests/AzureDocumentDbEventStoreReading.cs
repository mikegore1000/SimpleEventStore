using System.Threading.Tasks;
using SimpleEventStore.Tests;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    public class AzureDocumentDbEventStoreReading : EventStoreReading
    {
        protected override Task<IStorageEngine> CreateStorageEngine()
        {
            return StorageEngineFactory.Create();
        }
    }
}
