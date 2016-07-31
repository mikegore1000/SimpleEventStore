using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SimpleEventStore.Tests.Events;
using SimpleEventStore.Tests.Metadata;

namespace SimpleEventStore.Tests
{
    [TestFixture]
    public class EventStoreAppending
    {
        private const string StreamId = "TEST-ORDER";
        private SimpleEventStore.InMemoryStorageEngine engine;
        private EventStore subject;

        [SetUp]
        public void SetUp()
        {
            engine = new SimpleEventStore.InMemoryStorageEngine();
            subject = new EventStore(engine);
        }

        [Test]
        public async Task when_appending_to_a_new_stream_the_event_is_saved()
        {
            var @event = new EventData(new OrderCreated(StreamId));

            await subject.AppendToStream(StreamId, 0, @event);

            var stream = await subject.ReadStreamForwards(StreamId);
            Assert.That(stream.Count, Is.EqualTo(1));
            Assert.That(stream.Single().StreamId, Is.EqualTo(StreamId));
            Assert.That(stream.Single().EventBody, Is.EqualTo(@event.Body));
            Assert.That(stream.Single().EventNumber, Is.EqualTo(1));
        }

        [Test]
        public async Task when_appending_to_an_existing_stream_the_event_is_saved()
        {
            await subject.AppendToStream(StreamId, 0, new EventData(new OrderCreated(StreamId)));

            var @event = new EventData(new OrderDispatched(StreamId));

            await subject.AppendToStream(StreamId, 1, @event);

            var stream = await subject.ReadStreamForwards(StreamId);
            Assert.That(stream.Count, Is.EqualTo(2));
            Assert.That(stream.Skip(1).Single().EventBody, Is.EqualTo(@event.Body));
            Assert.That(stream.Skip(1).Single().EventNumber, Is.EqualTo(2));
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void when_appending_to_a_new_stream_with_an_unexpected_version__a_concurrency_error_is_thrown(int expectedVersion)
        {
            var @event = new EventData(new OrderDispatched(StreamId));

            Assert.ThrowsAsync<ConcurrencyException>(async () => await subject.AppendToStream(StreamId, expectedVersion, @event));
        }

        [TestCase(0)]
        [TestCase(2)]
        public async Task when_appending_to_an_existing_stream_with_an_unexpected_version_a_concurrency_error_is_thrown(int expectedVersion)
        {
            await subject.AppendToStream(StreamId, 0, new EventData(new OrderCreated(StreamId)));

            var @event = new EventData(new OrderDispatched(StreamId));

            Assert.ThrowsAsync<ConcurrencyException>(async () => await subject.AppendToStream(StreamId, expectedVersion, @event));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("$all")]
        public void when_appending_to_an_invalid_stream_id_an_argument_error_is_thrown(string streamId)
        {
            Assert.ThrowsAsync<ArgumentException>(async () => await subject.AppendToStream(streamId, 0, new EventData(new OrderCreated(streamId))));
        }

        [Test]
        public async Task when_appending_to_a_new_stream_with_multiple_events_then_they_are_saved()
        {
            var @events = new []
            {
                new EventData(new OrderCreated(StreamId)),
                new EventData(new OrderDispatched(StreamId))
            };

            await subject.AppendToStream(StreamId, 0, @events);

            var savedEvents = await subject.ReadStreamForwards(StreamId);

            Assert.That(savedEvents.Count(), Is.EqualTo(2));
            Assert.That(savedEvents.First().StreamId, Is.EqualTo(StreamId));
            Assert.That(savedEvents.First().EventNumber, Is.EqualTo(1));
            Assert.That(savedEvents.Skip(1).Single().StreamId, Is.EqualTo(StreamId));
            Assert.That(savedEvents.Skip(1).Single().EventNumber, Is.EqualTo(2));
        }

        [Test]
        public async Task when_appending_to_a_new_stream_the_event_metadata_is_saved()
        {
            var metadata = new TestMetadata { Value = "Hello" };
            var @event = new EventData(new OrderCreated(StreamId), metadata);

            await subject.AppendToStream(StreamId, 0, @event);

            var stream = await subject.ReadStreamForwards(StreamId);
            Assert.That(stream.Single().Metadata, Is.EqualTo(metadata));
        }
    }
}
