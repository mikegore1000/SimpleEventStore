using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SimpleEventStore.Tests.Events;

namespace SimpleEventStore.Tests
{
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

            await subject.AppendToStream(StreamId, new OrderCreated(StreamId), 0);
            await subject.AppendToStream(StreamId, new OrderDispatched(StreamId), 1);
        }

        [Test]
        public async Task when_reading_a_stream_all_events_are_returned()
        {
            var events = await subject.ReadStreamForwards(StreamId);

            Assert.That(events.Count(), Is.EqualTo(2));
            Assert.That(events.First().EventBody, Is.TypeOf<OrderCreated>());
            Assert.That(events.Skip(1).Single().EventBody, Is.TypeOf<OrderDispatched>());
        }
    }
}
