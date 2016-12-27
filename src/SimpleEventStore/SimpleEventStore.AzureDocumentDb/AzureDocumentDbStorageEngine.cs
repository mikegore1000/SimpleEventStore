using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SimpleEventStore.AzureDocumentDb
{
    public class AzureDocumentDbStorageEngine : IStorageEngine
    {
        private readonly IDocumentClient client;
        private readonly string databaseName;
        private readonly Uri commitsLink;

        public AzureDocumentDbStorageEngine(IDocumentClient client, string databaseName)
        {
            this.client = client;
            this.databaseName = databaseName;
            this.commitsLink = UriFactory.CreateDocumentCollectionUri(databaseName, "Commits");
        }

        public async Task Initialise()
        {
            await CreateDatabaseIfItDoesNotExist();
            await CreateCollectionIfItDoesNotExist();
        }

        public async Task AppendToStream(string streamId, IEnumerable<StorageEvent> events)
        {
            //TODO: Provide support for a multi-event commit (via a stored procedure)
            var @event = events.First();
            var maxRevisionQuery =
                this.client.CreateDocumentQuery<DocumentDbStorageEvent>(commitsLink)
                    .Where(x => x.StreamId == @event.StreamId)
                    .OrderByDescending(x => x.EventNumber)
                    .Select(x => x.EventNumber)
                    .Take(1)
                    .AsDocumentQuery();

            int maxRevision = (await maxRevisionQuery.ExecuteNextAsync<int>()).FirstOrDefault();

            if (@event.EventNumber == maxRevision + 1)
            {
                await this.client.CreateDocumentAsync(
                    commitsLink,
                    DocumentDbStorageEvent.FromStorageEvent(@event), 
                    disableAutomaticIdGeneration: true);
            }
            else
            {
                throw new ConcurrencyException($"Expected revision {maxRevision}, actual {@event.EventNumber}");
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
                .Where(x => x.Id == "Commits")
                .Take(1)
                .AsDocumentQuery();

            if (!(await commitsCollectionQuery.ExecuteNextAsync<DocumentCollection>()).Any())
            {

                // TODO: Need to optimise the indexing policy
                var collection = new DocumentCollection();
                collection.Id = "Commits";
                collection.PartitionKey.Paths.Add("/streamId");

                // TODO: Make this configurable by the consuming app - need to see if this can be updated, if so then we should attempt to update
                var requestOptions = new RequestOptions
                {
                    OfferThroughput = 10100 // Ensure it's partitioned
                };

                await client.CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(databaseName), collection, requestOptions);
            }
        }

        private class DocumentDbStorageEvent
        {
            [JsonProperty("id")]
            public string Id { get; set;  }

            [JsonProperty("eventId")]
            public Guid EventId { get; set; }

            [JsonProperty("body")]
            public JObject Body { get; set; }

            [JsonProperty("bodyType")]
            public string BodyType { get; set; }

            [JsonProperty("metadata")]
            public JObject Metadata { get; set; }

            [JsonProperty("metadataType")]
            public string MetadataType { get; set; }

            [JsonProperty("streamId")]
            public string StreamId { get; set; }

            [JsonProperty("eventNumber")]
            public int EventNumber { get; set; }

            public static DocumentDbStorageEvent FromStorageEvent(StorageEvent @event)
            {
                var docDbEvent = new DocumentDbStorageEvent();
                docDbEvent.Id = $"{@event.StreamId}:{@event.EventNumber}";
                docDbEvent.EventId = @event.EventId;
                docDbEvent.Body = JObject.FromObject(@event.EventBody);
                docDbEvent.BodyType = @event.EventBody.GetType().AssemblyQualifiedName;
                if (@event.Metadata != null)
                {
                    docDbEvent.Metadata = JObject.FromObject(@event.Metadata);
                    docDbEvent.MetadataType = @event.Metadata.GetType().AssemblyQualifiedName;
                }
                docDbEvent.StreamId = @event.StreamId;
                docDbEvent.EventNumber = @event.EventNumber;

                return docDbEvent;
            }

            public StorageEvent ToStorageEvent()
            {
                object body = Body.ToObject(Type.GetType(BodyType));
                object metadata = Metadata?.ToObject(Type.GetType(MetadataType));
                return new StorageEvent(StreamId, new EventData(EventId, body, metadata), EventNumber);
            }
        }
    }
}
