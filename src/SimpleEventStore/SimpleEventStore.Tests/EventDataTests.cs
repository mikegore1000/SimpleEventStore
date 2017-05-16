using System;
using Xunit;

namespace SimpleEventStore.Tests
{
    public class EventDataTests
    {
        [Fact]
        public void when_creating_an_instance_the_event_body_must_be_supplied()
        {
            Assert.Throws<ArgumentNullException>(() => new EventData(Guid.NewGuid(), null));
        }

        [Fact]
        public void when_creating_an_instance_the_properties_are_mapped()
        {
            var eventId = Guid.NewGuid();
            var sut =  new EventData(eventId, "BODY", "METADATA");

            Assert.Equal(eventId, sut.EventId);
            Assert.Equal("BODY", sut.Body);
            Assert.Equal("METADATA", sut.Metadata);
        }
    }
}
