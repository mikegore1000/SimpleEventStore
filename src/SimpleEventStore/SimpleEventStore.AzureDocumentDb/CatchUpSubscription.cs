using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace SimpleEventStore.AzureDocumentDb
{
    // TODO: Highly experimental - needs to be tested and API thought about
    public class CatchUpSubscription
    {
        private readonly IDocumentClient client;
        private readonly Uri collectionLink;
        private readonly int batchSize;

        public CatchUpSubscription(IDocumentClient client, string databaseName, int batchSize)
        {
            this.client = client;
            this.collectionLink = UriFactory.CreateDocumentCollectionUri(databaseName, "Commits");
            this.batchSize = batchSize;
        }

        public async Task ReadEvents(Action<string, StorageEvent> onNextEvent)
        {
            FeedResponse<dynamic> feedResponse;
            string checkpointToken = null;

            do
            {
                feedResponse = await client.ReadDocumentFeedAsync(collectionLink, new FeedOptions
                {
                    MaxItemCount = batchSize,
                    RequestContinuation = checkpointToken
                });

                checkpointToken = feedResponse.ResponseContinuation;

                foreach (var @event in feedResponse.OfType<Document>())
                {
                    onNextEvent(checkpointToken, DocumentDbStorageEvent.FromDocument(@event).ToStorageEvent());
                }
            } while (feedResponse.ResponseContinuation != null);
        }
    }
}
