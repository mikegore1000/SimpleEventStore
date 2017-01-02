using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace SimpleEventStore.AzureDocumentDb
{
    public class AzureDocumentDbStorageEngine : IStorageEngine
    {
        private const string CommitsCollectionName = "Commits";
        private const string AppendStoredProcedureName = "appendToStream";
        private const string ConcurrencyConflictErrorKey = "Concurrency conflict.";

        private readonly IDocumentClient client;
        private readonly string databaseName;
        private readonly ConsistencyLevel consistencyLevel;
        private readonly Uri commitsLink;
        private readonly Uri storedProcLink;
        private readonly List<Subscription> subscriptions = new List<Subscription>();

        public AzureDocumentDbStorageEngine(IDocumentClient client, string databaseName, ConsistencyLevel consistencyLevel)
        {
            this.client = client;
            this.databaseName = databaseName;
            this.consistencyLevel = consistencyLevel;
            this.commitsLink = UriFactory.CreateDocumentCollectionUri(databaseName, CommitsCollectionName);
            this.storedProcLink = UriFactory.CreateStoredProcedureUri(databaseName, CommitsCollectionName, AppendStoredProcedureName);
        }

        public async Task Initialise()
        {
            await CreateDatabaseIfItDoesNotExist();
            await CreateCollectionIfItDoesNotExist();
            await CreateAppendStoredProcedureIfItDoesNotExist();
        }

        public async Task AppendToStream(string streamId, IEnumerable<StorageEvent> events)
        {
            // TODO: Check RequestOptions - especially around consistency levels in case these should be specified

            var docs = events.Select(d => DocumentDbStorageEvent.FromStorageEvent(d)).ToList();

            try
            {
                var result = await this.client.ExecuteStoredProcedureAsync<dynamic>(
                    storedProcLink, 
                    new RequestOptions { PartitionKey = new PartitionKey(streamId), ConsistencyLevel = this.consistencyLevel },
                    docs);
            }
            catch (DocumentClientException ex)
            {
                if (ex.Error.Message.Contains(ConcurrencyConflictErrorKey))
                {
                    throw new ConcurrencyException(ex.Error.Message, ex);
                }

                throw;
            }
        }

        public async Task<IEnumerable<StorageEvent>> ReadStreamForwards(string streamId, int startPosition, int numberOfEventsToRead)
        {
            int endPosition = numberOfEventsToRead == int.MaxValue ? int.MaxValue : startPosition + numberOfEventsToRead;

            var eventsQuery = this.client.CreateDocumentQuery<DocumentDbStorageEvent>(commitsLink)
                .Where(x => x.StreamId == streamId && x.EventNumber >= startPosition && x.EventNumber <= endPosition)
                .OrderBy(x => x.EventNumber)
                .AsDocumentQuery();

            var events = new List<StorageEvent>();

            while (eventsQuery.HasMoreResults)
            {
                foreach (var e in await eventsQuery.ExecuteNextAsync<DocumentDbStorageEvent>())
                {
                    events.Add(e.ToStorageEvent());
                }
            }

            return events.AsReadOnly();
        }

        private async Task CreateDatabaseIfItDoesNotExist()
        {
            var databaseExistsQuery = client.CreateDatabaseQuery()
                .Where(x => x.Id == databaseName)
                .Take(1)
                .AsDocumentQuery();

            if (!(await databaseExistsQuery.ExecuteNextAsync<Database>()).Any())
            {
                await client.CreateDatabaseAsync(new Database {Id = databaseName});
            }
        }

        private async Task CreateCollectionIfItDoesNotExist()
        {
            var databaseUri = UriFactory.CreateDatabaseUri(databaseName);

            var commitsCollectionQuery = client.CreateDocumentCollectionQuery(databaseUri)
                .Where(x => x.Id == CommitsCollectionName)
                .Take(1)
                .AsDocumentQuery();

            if (!(await commitsCollectionQuery.ExecuteNextAsync<DocumentCollection>()).Any())
            {
                var collection = new DocumentCollection();
                collection.Id = CommitsCollectionName;
                collection.PartitionKey.Paths.Add("/streamId");
                collection.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
                collection.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/body/*"});
                collection.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/metadata/*" });

                // TODO: Make this configurable by the consuming app - need to see if this can be updated, if so then we should attempt to update
                var requestOptions = new RequestOptions
                {
                    OfferThroughput = 10100 // Ensure it's partitioned
                };

                await client.CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(databaseName), collection, requestOptions);
            }
        }

        private async Task CreateAppendStoredProcedureIfItDoesNotExist()
        {
            var query = client.CreateStoredProcedureQuery(commitsLink)
                .Where(x => x.Id == AppendStoredProcedureName)
                .AsDocumentQuery();

            if (!(await query.ExecuteNextAsync<StoredProcedure>()).Any())
            { 
                await client.CreateStoredProcedureAsync(commitsLink, new StoredProcedure
                {
                    Id = AppendStoredProcedureName,
                    Body = Scripts.appendToStream
                });
            }
        }

        public void SubscribeToAll(Action<string, StorageEvent> onNextEvent, string checkpoint)
        {
            Guard.IsNotNull(nameof(onNextEvent), onNextEvent);

            var subscription = new Subscription(this.client, this.commitsLink, onNextEvent, checkpoint);
            subscriptions.Add(subscription);

            subscription.Start();
        }

        private class Subscription
        {
            private readonly IDocumentClient client;
            private readonly Uri commitsLink;
            private readonly Action<string, StorageEvent> onNextEvent;
            private string checkpoint;
            private Task workerTask;

            public Subscription(IDocumentClient client, Uri commitsLink, Action<string, StorageEvent> onNextEvent, string checkpoint)
            {
                this.client = client;
                this.commitsLink = commitsLink;
                this.onNextEvent = onNextEvent;
                this.checkpoint = checkpoint;
            }

            // TODO: Configure the polling & any retry policy, also allow the subscription to be canclled (use a CancellationToken)
            public void Start()
            {
                workerTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        await ReadEvents();
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                });
            }

            private async Task ReadEvents()
            {
                FeedResponse<dynamic> feedResponse;
                do
                {
                    feedResponse = await client.ReadDocumentFeedAsync(commitsLink, new FeedOptions
                    {
                        MaxItemCount = 100, // TODO: Make this configurable
                        RequestContinuation = checkpoint
                    });

                    foreach (var @event in feedResponse.OfType<Document>())
                    {
                        onNextEvent(feedResponse.ResponseContinuation, DocumentDbStorageEvent.FromDocument(@event).ToStorageEvent());
                    }

                    checkpoint = feedResponse.ResponseContinuation;
                } while (feedResponse.ResponseContinuation != null);
            }
        }
    }
}
