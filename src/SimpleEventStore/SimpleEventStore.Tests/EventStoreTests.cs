using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SimpleEventStore.Tests
{
    [TestFixture]
    public class EventStoreTests
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

            var stream = engine.GetEventsForStream(StreamId);
            Assert.That(stream.Count, Is.EqualTo(1));
            Assert.That(stream.Single().StreamId, Is.EqualTo(StreamId));
            Assert.That(stream.Single().EventBody, Is.EqualTo(@event));
            Assert.That(stream.Single().EventNumber, Is.EqualTo(1));
        }

        [Test]
        public async Task when_appending_to_an_existing_stream_the_events_are_saved()
        {
            var @event = new OrderCreated(StreamId);

            await subject.AppendToStream(StreamId, @event, 1);

            var stream = engine.GetEventsForStream(StreamId);
            Assert.That(stream.Count, Is.EqualTo(1));
            Assert.That(stream.Single().StreamId, Is.EqualTo(StreamId));
            Assert.That(stream.Single().EventBody, Is.EqualTo(@event));
            Assert.That(stream.Single().EventNumber, Is.EqualTo(2));
        }

        // TODO: Add missing write scenarios
        // 1. Concurrency error on append

        public class OrderCreated
        {
            public OrderCreated(string orderId)
            {
                OrderId = orderId;
            }

            public string OrderId { get; private set; }
        }
    }
}
