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
        private readonly Action<string, StorageEvent> onNextEvent;

        public CatchUpSubscription(IDocumentClient client, string databaseName, Action<string, StorageEvent> onNextEvent)
        {
            this.client = client;
            this.collectionLink = UriFactory.CreateDocumentCollectionUri(databaseName, "Commits");
            this.onNextEvent = onNextEvent;
        }

        public async Task ReadEvents()
        {
            var feedResponse = await client.ReadDocumentFeedAsync(collectionLink, new FeedOptions { MaxItemCount = 100 });

            foreach (var @event in feedResponse.OfType<Document>())
            {
                onNextEvent(feedResponse.ResponseContinuation, DocumentDbStorageEvent.FromDocument(@event).ToStorageEvent());
            }
        }
    }
}
