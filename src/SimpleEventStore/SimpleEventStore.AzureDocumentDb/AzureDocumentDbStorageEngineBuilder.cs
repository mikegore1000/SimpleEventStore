using System;
using Microsoft.Azure.Documents.Client;

namespace SimpleEventStore.AzureDocumentDb
{
    public class AzureDocumentDbStorageEngineBuilder
    {
        private readonly string databaseName;
        private readonly DocumentClient client;
        private readonly CollectionOptions collectionOptions = new CollectionOptions();
        private readonly LoggingOptions loggingOptions = new LoggingOptions();
        private SubscriptionOptions subscriptionOptions;

        public AzureDocumentDbStorageEngineBuilder(DocumentClient client, string databaseName)
        {
            Guard.IsNotNull(nameof(client), client);
            Guard.IsNotNullOrEmpty(nameof(databaseName), databaseName);

            this.client = client;
            this.databaseName = databaseName;
        }

        public AzureDocumentDbStorageEngineBuilder UseCollection(Action<CollectionOptions> action)
        {
            Guard.IsNotNull(nameof(action), action);

            action(collectionOptions);
            return this;
        }

        public AzureDocumentDbStorageEngineBuilder UseSubscriptions(Action<SubscriptionOptions> action)
        {
            Guard.IsNotNull(nameof(action), action);

            subscriptionOptions = new SubscriptionOptions();
            action(subscriptionOptions);
            return this;
        }

        public AzureDocumentDbStorageEngineBuilder UseLogging(Action<LoggingOptions> action)
        {
            Guard.IsNotNull(nameof(action), action);

            action(loggingOptions);
            return this;
        }

        public IStorageEngine Build()
        {
            var engine = new AzureDocumentDbStorageEngine(this.client, this.databaseName, this.collectionOptions, this.subscriptionOptions, this.loggingOptions);
            return engine;
        }
    }
}
