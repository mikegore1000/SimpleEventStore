using System;
using NUnit.Framework;

namespace SimpleEventStore.Tests
{
    [TestFixture]
    public class StorageEventTests
    {
        [Test]
        public void when_creating_a_new_instance_the_properties_are_mapped()
        {
            var eventId = Guid.NewGuid();
            var @event = new EventData(eventId, "BODY", "METADATA");

            var sut = new StorageEvent("STREAMID", @event, 1);

            Assert.That(sut.StreamId, Is.EqualTo("STREAMID"));
            Assert.That(sut.EventBody, Is.EqualTo("BODY"));
            Assert.That(sut.Metadata, Is.EqualTo("METADATA"));
            Assert.That(sut.EventNumber, Is.EqualTo(1));
            Assert.That(sut.EventId, Is.EqualTo(eventId));
        }
    }
}
