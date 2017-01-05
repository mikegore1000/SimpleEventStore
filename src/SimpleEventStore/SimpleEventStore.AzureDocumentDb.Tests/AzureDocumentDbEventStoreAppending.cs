using System;
using SimpleEventStore.Tests;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    public class AzureDocumentDbEventStoreAppending : EventStoreAppending
    {
        protected override IStorageEngine CreateStorageEngine()
        {
            return StorageEngineFactory.Create("AppendingTests").Result;
        }
    }
}
