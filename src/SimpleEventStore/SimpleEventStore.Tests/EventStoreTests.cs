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

            Assert.That(engine.GetEventsForStream(streamId).Single(), Is.EqualTo(@event));
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
        private readonly Dictionary<string, List<object>> streams = new Dictionary<string, List<object>>();

        public Task AppendToStream(string streamId, object @event)
        {
            if (!streams.ContainsKey(streamId))
            {
                streams[streamId] = new List<object>();
            }

            streams[streamId].Add(@event);

            return Task.FromResult(0);
        }

        public List<object> GetEventsForStream(string streamId)
        {
            return streams[streamId];
        }
    }
}
