using System;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace SimpleEventStore.CosmosDb
{
    public class AzureCosmosDbStorageEngineBuilder
    {
        private readonly string _databaseName;
        private readonly CosmosClient _client;
        private readonly CollectionOptions collectionOptions = new CollectionOptions();
        private readonly DatabaseOptions _databaseOptions = new DatabaseOptions();
        private readonly LoggingOptions _loggingOptions = new LoggingOptions();
        private ISerializationTypeMap _typeMap = new DefaultSerializationTypeMap();
        private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings();

        public AzureCosmosDbStorageEngineBuilder(CosmosClient client, string databaseName)
        {
            Guard.IsNotNull(nameof(client), client);
            Guard.IsNotNullOrEmpty(nameof(databaseName), databaseName);

            _client = client;
            _databaseName = databaseName;
        }

        public AzureCosmosDbStorageEngineBuilder UseCollection(Action<CollectionOptions> action)
        {
            Guard.IsNotNull(nameof(action), action);

            action(collectionOptions);
            return this;
        }

        public AzureCosmosDbStorageEngineBuilder UseLogging(Action<LoggingOptions> action)
        {
            Guard.IsNotNull(nameof(action), action);

            action(_loggingOptions);
            return this;
        }

        public AzureCosmosDbStorageEngineBuilder UseTypeMap(ISerializationTypeMap typeMap)
        {
            Guard.IsNotNull(nameof(typeMap), typeMap);
            _typeMap = typeMap;

            return this;
        }

        public AzureCosmosDbStorageEngineBuilder UseJsonSerializerSettings(JsonSerializerSettings settings)
        {
            Guard.IsNotNull(nameof(settings), settings);
            _jsonSerializerSettings = settings;
            return this;
        }

        public AzureCosmosDbStorageEngineBuilder UseDatabase(Action<DatabaseOptions> action)
        {
            Guard.IsNotNull(nameof(action), action);

            action(_databaseOptions);
            return this;
        }

        public IStorageEngine Build()
        {
            return new AzureCosmosDbStorageEngine(_client, 
                _databaseName, 
                collectionOptions,
                _databaseOptions,
                _loggingOptions,
                _typeMap,
                JsonSerializer.Create(_jsonSerializerSettings));
        }
    }
}
