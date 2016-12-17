using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SimpleEventStore.AzureDocumentDb
{
    public class AzureDocumentDbStorageEngine : IStorageEngine
    {
        private readonly IDocumentClient client;
        private readonly string databaseName;
        private Database database;
        private DocumentCollection commitsCollection;

        public AzureDocumentDbStorageEngine(IDocumentClient client, string databaseName)
        {
            this.client = client;
            this.databaseName = databaseName;
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
            var maxRevisionArr = this.client.CreateDocumentQuery<DocumentDbStorageEvent>(commitsCollection.DocumentsLink)
                .Where(x => x.StreamId == @event.StreamId)
                .ToArray();

            var maxRevision = maxRevisionArr.Length == 0 ? 0 : maxRevisionArr.Max(x => x.EventNumber);

            if (@event.EventNumber == maxRevision + 1)
            {
                await this.client.CreateDocumentAsync(
                    commitsCollection.DocumentsLink,
                    DocumentDbStorageEvent.FromStorageEvent(@event), 
                    disableAutomaticIdGeneration: true);
            }
            else
            {
                throw new ConcurrencyException($"Expected revision {maxRevision}, actual {@event.EventNumber}");
            }
        }

        public Task<IEnumerable<StorageEvent>> ReadStreamForwards(string streamId, int startPosition, int numberOfEventsToRead)
        {
            int endPosition = numberOfEventsToRead == int.MaxValue ? int.MaxValue : startPosition + numberOfEventsToRead;

            // TODO: Read more into the execute next async method - looks like you perform a do while loop
            // See: https://vincentlauzon.com/2015/01/06/documentdb-async-querying-streaming/
            var events = this.client.CreateDocumentQuery<DocumentDbStorageEvent>(commitsCollection.DocumentsLink)
                .Where(x => x.StreamId == streamId && x.EventNumber >= startPosition && x.EventNumber <= endPosition)
                .OrderBy(x => x.EventNumber)
                .ToArray();

            return Task.FromResult(events.Select(x => x.ToStorageEvent()));
        }

        private async Task CreateDatabaseIfItDoesNotExist()
        {
            this.database = client.CreateDatabaseQuery().Where(x => x.Id == databaseName).ToArray().SingleOrDefault();

            if (this.database == null)
            {
                this.database = await client.CreateDatabaseAsync(new Database { Id = databaseName });
            }
        }

        private async Task CreateCollectionIfItDoesNotExist()
        {
            commitsCollection = client.CreateDocumentCollectionQuery(this.database.CollectionsLink)
                .ToArray()
                .SingleOrDefault(x => x.Id == "Commits");

            // TODO: Need to optimise the indexing policy
            if (commitsCollection == null)
            {
                var collection = new DocumentCollection();
                collection.Id = "Commits";
                collection.PartitionKey.Paths.Add("/streamId");

                // TODO: Make this configurable by the consuming app - need to see if this can be updated, if so then we should attempt to update
                var requestOptions = new RequestOptions
                {
                    OfferThroughput = 10100 // Ensure it's partitioned
                };

                commitsCollection = await client.CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(databaseName), collection, requestOptions);
            }
        }

        private class DocumentDbStorageEvent
        {
            [JsonProperty("id")]
            public Guid Id { get; set;  }

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
                docDbEvent.Id = @event.EventId;
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
                return new StorageEvent(StreamId, new EventData(Id, body, metadata), EventNumber);
            }
        }
    }
}
