using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SimpleEventStore.Tests.Events;

namespace SimpleEventStore.Tests
{
    [TestFixture]
    public class EventStoreAllStreamReading
    {
        private IStorageEngine engine;
        private EventStore subject;


        [SetUp]
        public async Task SetUp()
        {
            engine = new StorageEngineFake();
            subject = new EventStore(engine);

            await subject.AppendToStream("STREAM-1", 0, new EventData(new OrderCreated("STREAM-1")));
            await subject.AppendToStream("STREAM-2", 0, new EventData(new OrderCreated("STREAM-2")));
            await subject.AppendToStream("STREAM-2", 1, new EventData(new OrderDispatched("STREAM-2")));
            await subject.AppendToStream("STREAM-1", 1, new EventData(new OrderDispatched("STREAM-1")));
        }

        [Test]
        public async Task when_reading_from_the_all_stream_events_are_returned_in_the_order_they_were_written_to_the_store()
        {
            var events = await subject.ReadStreamForwards("$all");

            Assert.That(events.Count(), Is.EqualTo(4));
            Assert.That(events.First().StreamId, Is.EqualTo("STREAM-1"));
            Assert.That(events.First().EventBody, Is.TypeOf<OrderCreated>());
            Assert.That(events.Skip(1).First().StreamId, Is.EqualTo("STREAM-2"));
            Assert.That(events.Skip(1).First().EventBody, Is.TypeOf<OrderCreated>());
            Assert.That(events.Skip(2).First().StreamId, Is.EqualTo("STREAM-2"));
            Assert.That(events.Skip(2).First().EventBody, Is.TypeOf<OrderDispatched>());
            Assert.That(events.Skip(3).First().StreamId, Is.EqualTo("STREAM-1"));
            Assert.That(events.Skip(3).First().EventBody, Is.TypeOf<OrderDispatched>());
        }

        [Test]
        public async Task when_reading_from_the_all_steam_only_the_required_events_are_returned()
        {
            var events = await subject.ReadStreamForwards("$all", 2, 2);

            Assert.That(events.Count(), Is.EqualTo(2));
            Assert.That(events.First().StreamId, Is.EqualTo("STREAM-2"));
            Assert.That(events.First().EventBody, Is.TypeOf<OrderCreated>());
            Assert.That(events.Skip(1).First().StreamId, Is.EqualTo("STREAM-2"));
            Assert.That(events.Skip(1).First().EventBody, Is.TypeOf<OrderDispatched>());
        }
    }
}
