using System;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SimpleEventStore.CosmosDb
{
    public class CosmosDbStorageEvent
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

        public static CosmosDbStorageEvent FromStorageEvent(StorageEvent @event, ISerializationTypeMap typeMap, JsonSerializer serializer)
        {
            var docDbEvent = new CosmosDbStorageEvent();
            docDbEvent.Id = $"{@event.StreamId}:{@event.EventNumber}";
            docDbEvent.EventId = @event.EventId;
            docDbEvent.Body = JObject.FromObject(@event.EventBody, serializer);
            docDbEvent.BodyType = typeMap.GetNameFromType(@event.EventBody.GetType());
            if (@event.Metadata != null)
            {
                docDbEvent.Metadata = JObject.FromObject(@event.Metadata, serializer);
                docDbEvent.MetadataType = typeMap.GetNameFromType(@event.Metadata.GetType());
            }
            docDbEvent.StreamId = @event.StreamId;
            docDbEvent.EventNumber = @event.EventNumber;

            return docDbEvent;
        }

        public static CosmosDbStorageEvent FromDocument(ItemResponse<CosmosDbStorageEvent> document)
        {
            return document.Resource;
        }

        public StorageEvent ToStorageEvent(ISerializationTypeMap typeMap, JsonSerializer serializer)
        {
            object body = Body.ToObject(typeMap.GetTypeFromName(BodyType), serializer);
            object metadata = Metadata?.ToObject(typeMap.GetTypeFromName(MetadataType), serializer);
            return new StorageEvent(StreamId, new EventData(EventId, body, metadata), EventNumber);
        }
    }
}