using SimpleEventStore.Tests;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    public class AzureDocumentDbEventStoreReading : EventStoreReading
    {
        protected override IStorageEngine CreateStorageEngine()
        {
            return StorageEngineFactory.Create("ReadingTests").Result;
        }
    }
}
