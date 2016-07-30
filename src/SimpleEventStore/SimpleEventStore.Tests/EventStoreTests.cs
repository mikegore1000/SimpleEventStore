using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SimpleEventStore.Tests
{
    [TestFixture]
    public class EventStoreTests
    {
        [Test]
        public async Task when_appending_to_a_new_stream_the_events_are_saved()
        {
            var streamId = "TEST-ORDER";
            var @event = new OrderCreated(streamId);
            var engine = new StorageEngineFake();
            var subject = new EventStore(engine);

            await subject.AppendToStream(streamId, @event);

            var stream = engine.GetEventsForStream(streamId);

            Assert.That(stream.Count, Is.EqualTo(1));
            Assert.That(stream.Single().StreamId, Is.EqualTo(streamId));
            Assert.That(stream.Single().EventBody, Is.EqualTo(@event));
        }

        public class OrderCreated
        {
            public OrderCreated(string orderId)
            {
                OrderId = orderId;
            }

            public string OrderId { get; private set; }
        }
    }

    internal class StorageEngineFake : IStorageEngine
    {
        private readonly Dictionary<string, List<StorageEvent>> streams = new Dictionary<string, List<StorageEvent>>();

        public Task AppendToStream(string streamId, object @event)
        {
            if (!streams.ContainsKey(streamId))
            {
                streams[streamId] = new List<StorageEvent>();
            }

            streams[streamId].Add(new StorageEvent(streamId, @event));

            return Task.FromResult(0);
        }

        public List<StorageEvent> GetEventsForStream(string streamId)
        {
            return streams[streamId];
        }
    }
}
