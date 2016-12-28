using System;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SimpleEventStore.AzureDocumentDb
{
    internal class DocumentDbStorageEvent
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

        public static DocumentDbStorageEvent FromDocument(Document document)
        {
            var docDbEvent = new DocumentDbStorageEvent
            {
                Id = document.GetPropertyValue<string>("id"),
                EventId = document.GetPropertyValue<Guid>("eventId"),
                Body = document.GetPropertyValue<JObject>("body"),
                BodyType = document.GetPropertyValue<string>("bodyType"),
                Metadata = document.GetPropertyValue<JObject>("metadata"),
                MetadataType = document.GetPropertyValue<string>("metadataType"),
                StreamId = document.GetPropertyValue<string>("streamId"),
                EventNumber = document.GetPropertyValue<int>("eventNumber")
            };

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