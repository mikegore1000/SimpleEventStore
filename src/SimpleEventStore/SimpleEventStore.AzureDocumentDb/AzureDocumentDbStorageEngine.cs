using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Uri commitsLink;
        private readonly Uri storedProcLink;

        public AzureDocumentDbStorageEngine(IDocumentClient client, string databaseName)
        {
            this.client = client;
            this.databaseName = databaseName;
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
                    new RequestOptions { PartitionKey = new PartitionKey(streamId) },
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

                // TODO: Need to optimise the indexing policy
                var collection = new DocumentCollection();
                collection.Id = CommitsCollectionName;
                collection.PartitionKey.Paths.Add("/streamId");

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
    }
}
