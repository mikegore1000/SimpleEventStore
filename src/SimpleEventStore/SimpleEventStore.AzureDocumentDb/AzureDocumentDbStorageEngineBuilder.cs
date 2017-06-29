using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

namespace SimpleEventStore.AzureDocumentDb
{
    public class AzureDocumentDbStorageEngineBuilder
    {
        private readonly string databaseName;
        private readonly DocumentClient client;
        private readonly CollectionOptions collectionOptions = new CollectionOptions();
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

        public IStorageEngine Build()
        {
            var engine = new AzureDocumentDbStorageEngine(this.client, databaseName, collectionOptions, subscriptionOptions);
            return engine;
        }
    }
}
