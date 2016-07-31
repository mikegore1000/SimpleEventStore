using System;
using NUnit.Framework;
using SimpleEventStore.Tests.Events;

namespace SimpleEventStore.Tests
{
    [TestFixture]
    public class EventDataTests
    {
        private const string StreamId = "TEST-ORDER";

        [Test]
        public void when_creating_an_instance_the_event_body_must_be_supplied()
        {
            Assert.Throws<ArgumentException>(() => new EventData(null));
        }

        [Test]
        public void when_creating_an_instance_the_event_body_and_metadata_must_be_supplied()
        {
            Assert.Throws<ArgumentException>(() => new EventData(null, null));
            Assert.Throws<ArgumentException>(() => new EventData(new OrderCreated(StreamId), null));
        }
    }
}
