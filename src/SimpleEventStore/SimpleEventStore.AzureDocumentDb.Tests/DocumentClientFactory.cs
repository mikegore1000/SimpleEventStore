using System;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    internal static class DocumentClientFactory
    {
        internal static DocumentClient Create(string databaseName)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var documentDbUri = config["Uri"];
            var authKey = config["AuthKey"];

            return new DocumentClient(new Uri(documentDbUri), authKey);
        }
    }
}