using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using SimpleEventStore.Tests;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    public class AzureDocumentDbEventStoreAppending : EventStoreAppending
    {
        protected async override Task<IStorageEngine> CreateStorageEngine()
        {
            var documentDbUri = "https://mg-eventsourcing-simple.documents.azure.com:443/";
            var authKey = "9FbXSIuFp420lalYtSsUmA9TNscZqsvseuSESRDW5saqaQxUjiv5UNGgxz2ODxKvfKIv4dKrzCVfspg97JDBTQ==";
            var databaseName = "DocumentDbEventStoreTests";
            DocumentClient client = new DocumentClient(new Uri(documentDbUri), authKey);

            var storageEngine = new AzureDocumentDbStorageEngine(client, databaseName);
            await storageEngine.Initialise();

            return storageEngine;
        }
    }
}
