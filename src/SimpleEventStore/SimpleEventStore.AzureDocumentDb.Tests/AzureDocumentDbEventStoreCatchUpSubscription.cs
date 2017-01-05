using SimpleEventStore.Tests;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    public class AzureDocumentDbEventStoreCatchUpSubscription : EventStoreCatchUpSubscription
    {
        protected override IStorageEngine CreateStorageEngine()
        {
            return StorageEngineFactory.Create("CatchUpSubscriptionTests").Result;
        }
    }
}
