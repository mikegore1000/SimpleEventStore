using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SimpleEventStore.Tests.Events;
using Xunit;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    public class DocumentDbStorageEventTests
    {
        [Fact]
        public void when_converting_to_a_storage_event_it_succeeds()
        {
            var id = Guid.NewGuid();
            var body = new OrderCreated("TEST-ORDER");
            var metadata = new TestMetadata { Value = "TEST-VALUE" };
            var sut = new DocumentDbStorageEvent
            {
                StreamId = "TEST-STREAM",
                Body = JObject.FromObject(body),
                BodyType = "OrderCreated",
                Metadata = JObject.FromObject(metadata),
                MetadataType = "TestMetadata",
                EventNumber = 1,
                EventId = id
            };
            var typeMap = new ConfigurableSerializationTypeMap().RegisterTypes(
                typeof(OrderCreated).GetTypeInfo().Assembly,
                t => t.Namespace.EndsWith("Events"),
                t => t.Name);
            var result = sut.ToStorageEvent(typeMap);

            Assert.Equal(sut.StreamId, result.StreamId);
            Assert.Equal(body.OrderId, ((OrderCreated)result.EventBody).OrderId);
            Assert.Equal(metadata.Value, ((TestMetadata)result.Metadata).Value);
            Assert.Equal(sut.EventNumber, result.EventNumber);
            Assert.Equal(sut.EventId, result.EventId);
        }
    }
}
