using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace SimpleEventStore.CosmosDb.Tests
{
    internal static class CosmosClientFactory
    {
        internal static CosmosClient Create()
        {
            return Create(new JsonSerializerSettings());
        }

        internal static CosmosClient Create(JsonSerializerSettings serializationOptions)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            var documentDbUri = config["Uri"];
            var authKey = config["AuthKey"];

            return new CosmosClientBuilder(documentDbUri, authKey)
                .WithCustomSerializer(new CosmosJsonNetSerializer(serializationOptions))
                .Build();
        }
    }
}