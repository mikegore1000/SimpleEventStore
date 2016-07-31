using System.Linq;
using System.Threading.Tasks;
using SimpleEventStore.Tests.Events;
using Xunit;

namespace SimpleEventStore.Tests
{
    public abstract class EventStoreAllStreamReading
    {
        [Fact]
        public async Task when_reading_from_the_all_stream_events_are_returned_in_the_order_they_were_written_to_the_store()
        {
            var subject = CreateEventStore();

            await subject.AppendToStream("STREAM-1", 0, new EventData(new OrderCreated("STREAM-1")));
            await subject.AppendToStream("STREAM-2", 0, new EventData(new OrderCreated("STREAM-2")));
            await subject.AppendToStream("STREAM-2", 1, new EventData(new OrderDispatched("STREAM-2")));
            await subject.AppendToStream("STREAM-1", 1, new EventData(new OrderDispatched("STREAM-1")));

            var events = await subject.ReadStreamForwards("$all");

            Assert.Equal(4, events.Count());
            Assert.Equal("STREAM-1", events.First().StreamId);
            Assert.IsType<OrderCreated>(events.First().EventBody);
            Assert.Equal("STREAM-2", events.Skip(1).First().StreamId);
            Assert.IsType<OrderCreated>(events.Skip(1).First().EventBody);
            Assert.Equal("STREAM-2", events.Skip(2).First().StreamId);
            Assert.IsType<OrderDispatched>(events.Skip(2).First().EventBody);
            Assert.Equal("STREAM-1", events.Skip(3).First().StreamId);
            Assert.IsType<OrderDispatched>(events.Skip(3).First().EventBody);
        }

        [Fact]
        public async Task when_reading_from_the_all_steam_only_the_required_events_are_returned()
        {
            var subject = CreateEventStore();

            await subject.AppendToStream("STREAM-1", 0, new EventData(new OrderCreated("STREAM-1")));
            await subject.AppendToStream("STREAM-2", 0, new EventData(new OrderCreated("STREAM-2")));
            await subject.AppendToStream("STREAM-2", 1, new EventData(new OrderDispatched("STREAM-2")));
            await subject.AppendToStream("STREAM-1", 1, new EventData(new OrderDispatched("STREAM-1")));

            var events = await subject.ReadStreamForwards("$all", 2, 2);

            Assert.Equal(2, events.Count());
            Assert.Equal("STREAM-2", events.First().StreamId);
            Assert.IsType<OrderCreated>(events.First().EventBody);
            Assert.Equal("STREAM-2", events.Skip(1).First().StreamId);
            Assert.IsType<OrderDispatched>(events.Skip(1).First().EventBody);
        }

        private EventStore CreateEventStore()
        {
            return new EventStore(new InMemoryStorageEngine());
        }
    }
}
