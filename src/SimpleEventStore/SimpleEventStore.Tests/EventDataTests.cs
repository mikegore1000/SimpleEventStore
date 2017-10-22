using System;
using NUnit.Framework;

namespace SimpleEventStore.Tests
{
    [TestFixture]
    public class EventDataTests
    {
        [Test]
        public void when_creating_an_instance_the_event_body_must_be_supplied()
        {
            Assert.Throws<ArgumentNullException>(() => new EventData(Guid.NewGuid(), null));
        }

        [Test]
        public void when_creating_an_instance_the_properties_are_mapped()
        {
            var eventId = Guid.NewGuid();
            var sut =  new EventData(eventId, "BODY", "METADATA");

            Assert.That(sut.EventId, Is.EqualTo(eventId));
            Assert.That(sut.Body, Is.EqualTo("BODY"));
            Assert.That(sut.Metadata, Is.EqualTo("METADATA"));
        }
    }
}
