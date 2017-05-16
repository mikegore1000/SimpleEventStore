using System;
using Xunit;

namespace SimpleEventStore.Tests
{
    public class StorageEventTests
    {
        [Fact]
        public void when_creating_a_new_instance_the_properties_are_mapped()
        {
            var eventId = Guid.NewGuid();
            var @event = new EventData(eventId, "BODY", "METADATA");

            var sut = new StorageEvent("STREAMID", @event, 1);

            Assert.Equal("STREAMID", sut.StreamId);
            Assert.Equal("BODY", sut.EventBody);
            Assert.Equal("METADATA", sut.Metadata);
            Assert.Equal(1, sut.EventNumber);
            Assert.Equal(eventId, sut.EventId);
        }
    }
}
