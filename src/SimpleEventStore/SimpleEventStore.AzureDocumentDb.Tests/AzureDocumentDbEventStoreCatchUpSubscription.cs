using System.Threading.Tasks;
using SimpleEventStore.Tests;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    public class AzureDocumentDbEventStoreCatchUpSubscription : EventStoreCatchUpSubscription
    {
        protected override Task<IStorageEngine> CreateStorageEngine()
        {
            return StorageEngineFactory.Create();
        }
    }
}
