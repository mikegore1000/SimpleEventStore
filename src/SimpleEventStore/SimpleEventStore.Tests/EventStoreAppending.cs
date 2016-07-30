using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SimpleEventStore.Tests.Events;

namespace SimpleEventStore.Tests
{
    [TestFixture]
    public class EventStoreAppending
    {
        private const string StreamId = "TEST-ORDER";
        private StorageEngineFake engine;
        private EventStore subject;

        [SetUp]
        public void SetUp()
        {
            engine = new StorageEngineFake();
            subject = new EventStore(engine);
        }

        [Test]
        public async Task when_appending_to_a_new_stream_the_events_are_saved()
        {
            var @event = new OrderCreated(StreamId);

            await subject.AppendToStream(StreamId, @event, 0);

            var stream = await engine.ReadStreamForwards(StreamId);
            Assert.That(stream.Count, Is.EqualTo(1));
            Assert.That(stream.Single().StreamId, Is.EqualTo(StreamId));
            Assert.That(stream.Single().EventBody, Is.EqualTo(@event));
            Assert.That(stream.Single().EventNumber, Is.EqualTo(1));
        }

        [Test]
        public async Task when_appending_to_an_existing_stream_the_events_are_saved()
        {
            await subject.AppendToStream(StreamId, new OrderCreated(StreamId), 0);

            var @event = new OrderDispatched(StreamId);

            await subject.AppendToStream(StreamId, @event, 1);

            var stream = await engine.ReadStreamForwards(StreamId);

            Assert.That(stream.Count, Is.EqualTo(2));
            Assert.That(stream.Skip(1).Single().EventBody, Is.EqualTo(@event));
            Assert.That(stream.Skip(1).Single().EventNumber, Is.EqualTo(2));
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void when_appending_to_a_new_stream_with_an_unexpected_version__a_concurrency_error_is_thrown(int expectedVersion)
        {
            var @event = new OrderDispatched(StreamId);

            Assert.ThrowsAsync<ConcurrencyException>(async () => await subject.AppendToStream(StreamId, @event, expectedVersion));
        }

        [TestCase(0)]
        [TestCase(2)]
        public async Task when_appending_to_an_existing_stream_with_an_unexpected_version_a_concurrency_error_is_thrown(int expectedVersion)
        {
            await subject.AppendToStream(StreamId, new OrderCreated(StreamId), 0);

            var @event = new OrderDispatched(StreamId);

            Assert.ThrowsAsync<ConcurrencyException>(async () => await subject.AppendToStream(StreamId, @event, expectedVersion));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void when_appending_to_an_invalid_stream_id_an_argument_error_is_thrown(string streamId)
        {
            Assert.ThrowsAsync<ArgumentException>(async () => await subject.AppendToStream(streamId, new OrderCreated(streamId), 0));
        }

        // TODO: Append features to finish
        // 1. Shouldn't allow writing to the "$all" stream
        // 2. Allow writing multiple events
    }
}
