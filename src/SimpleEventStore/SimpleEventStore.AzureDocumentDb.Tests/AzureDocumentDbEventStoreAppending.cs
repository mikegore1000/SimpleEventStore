using System;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using SimpleEventStore.Tests;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    //public class AzureDocumentDbEventStoreAppending : EventStoreAppending
    //{
    //    protected async override Task<IStorageEngine> CreateStorageEngine()
    //    {
    //        var documentDbUri = "https://simple-event-store.documents.azure.com:443/";
    //        var authKey = "ehsCsRnC5MorINm4gUj6n6kozIVxtCh7z1NTPBZ6sBtY2hT9u23XiRnvSYnfKw0UzQZdSQ49PdL0BNKhKWjozw==";
    //        var databaseName = "DocumentDbEventStoreTests";
    //        DocumentClient client = new DocumentClient(new Uri(documentDbUri), authKey);

    //        var storageEngine = new AzureDocumentDbStorageEngine(client, databaseName);
    //        await storageEngine.Initialise();

    //        return storageEngine;
    //    }
    //}
}
