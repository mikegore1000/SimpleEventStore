using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SimpleEventStore.Tests.Events;

namespace SimpleEventStore.Tests
{
    // TODO: Add the remaining features
    // 1. Read an "$all" stream - requires a global checkpoint
    // 2. Allow stream versions to be 64 bit ints

    [TestFixture]
    public class EventStoreReading
    {
        private const string StreamId = "TEST-ORDER";
        private StorageEngineFake engine;
        private EventStore subject;

        [SetUp]
        public async Task SetUp()
        {
            engine = new StorageEngineFake();
            subject = new EventStore(engine);

            await subject.AppendToStream(StreamId, 0, new EventData(new OrderCreated(StreamId)));
            await subject.AppendToStream(StreamId, 1, new EventData(new OrderDispatched(StreamId)));
        }

        [Test]
        public async Task when_reading_a_stream_all_events_are_returned()
        {
            var events = await subject.ReadStreamForwards(StreamId);

            Assert.That(events.Count(), Is.EqualTo(2));
            Assert.That(events.First().EventBody, Is.TypeOf<OrderCreated>());
            Assert.That(events.Skip(1).Single().EventBody, Is.TypeOf<OrderDispatched>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void when_reading_from_an_invalid_stream_id_an_argument_error_is_thrown(string streamId)
        {
            Assert.ThrowsAsync<ArgumentException>(async () => await subject.ReadStreamForwards(streamId));
        }

        [Test]
        public async Task when_reading_a_stream_only_the_required_events_are_returned()
        {
            var events = await subject.ReadStreamForwards(StreamId, startPosition: 2, numberOfEventsToRead: 1);

            Assert.That(events.Count(), Is.EqualTo(1));
            Assert.That(events.First().EventBody, Is.TypeOf<OrderDispatched>());
        }
    }
}
