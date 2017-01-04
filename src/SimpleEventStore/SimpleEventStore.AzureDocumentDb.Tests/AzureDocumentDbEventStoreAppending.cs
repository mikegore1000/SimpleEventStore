using System.Threading.Tasks;
using SimpleEventStore.Tests;
using Xunit;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    [Collection("DocumentDb Integration Tests")]
    public class AzureDocumentDbEventStoreAppending : EventStoreAppending
    {
        protected override Task<IStorageEngine> CreateStorageEngine()
        {
            return StorageEngineFactory.Create();
        }
    }
}
