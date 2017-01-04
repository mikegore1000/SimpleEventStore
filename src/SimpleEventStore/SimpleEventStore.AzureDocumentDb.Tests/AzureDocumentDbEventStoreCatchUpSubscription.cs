using System.Threading.Tasks;
using SimpleEventStore.Tests;
using Xunit;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    [Collection("DocumentDb Integration Tests")]
    public class AzureDocumentDbEventStoreCatchUpSubscription : EventStoreCatchUpSubscription
    {
        protected override Task<IStorageEngine> CreateStorageEngine()
        {
            return StorageEngineFactory.Create();
        }
    }
}
