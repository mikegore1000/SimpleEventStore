using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    internal static class StorageEngineFactory
    {
        internal static async Task<IStorageEngine> Create(string databaseName)
        {
            var documentDbUri = ConfigurationManager.AppSettings["Uri"];
            var authKey = ConfigurationManager.AppSettings["AuthKey"];
            var consistencyLevel = ConfigurationManager.AppSettings["ConsistencyLevel"];
            ConsistencyLevel consistencyLevelEnum;

            if(!Enum.TryParse(consistencyLevel, true, out consistencyLevelEnum))
            {
                throw new Exception($"The ConsistencyLevel value {consistencyLevel} is not supported");
            }

            DocumentClient client = new DocumentClient(new Uri(documentDbUri), authKey);

            var storageEngine = new AzureDocumentDbStorageEngine(client, databaseName, new DatabaseOptions(consistencyLevelEnum, 400), new SubscriptionOptions(maxItemCount: 1, pollEvery: TimeSpan.FromSeconds(0.5)));
            await storageEngine.Initialise();

            return storageEngine;
        }
    }
}
