using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    internal static class StorageEngineFactory
    {
        internal static async Task<IStorageEngine> Create(string databaseName)
        {
            var documentDbUri = "https://localhost:8081/";
            var authKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            DocumentClient client = new DocumentClient(new Uri(documentDbUri), authKey);

            var storageEngine = new AzureDocumentDbStorageEngine(client, databaseName, new DatabaseOptions(ConsistencyLevel.BoundedStaleness, 400), new SubscriptionOptions(maxItemCount: 1, pollEvery: TimeSpan.FromSeconds(0.5)));
            await storageEngine.Initialise();

            return storageEngine;
        }
    }
}
