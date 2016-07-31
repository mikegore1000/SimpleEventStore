using System;
using System.Linq;
using System.Threading.Tasks;
using SimpleEventStore.Tests.Events;
using Xunit;

namespace SimpleEventStore.Tests
{
    public abstract class EventStoreReading : EventStoreTestBase
    {
        private const string StreamId = "TEST-ORDER";

        [Fact]
        public async Task when_reading_a_stream_all_events_are_returned()
        {
            var subject = CreateEventStore();

            await subject.AppendToStream(StreamId, 0, new EventData(new OrderCreated(StreamId)));
            await subject.AppendToStream(StreamId, 1, new EventData(new OrderDispatched(StreamId)));

            var events = await subject.ReadStreamForwards(StreamId);

            Assert.Equal(2, events.Count());
            Assert.IsType<OrderCreated>(events.First().EventBody);
            Assert.IsType<OrderDispatched>(events.Skip(1).Single().EventBody);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task when_reading_from_an_invalid_stream_id_an_argument_error_is_thrown(string streamId)
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => await CreateEventStore().ReadStreamForwards(streamId));
        }

        [Fact]
        public async Task when_reading_a_stream_only_the_required_events_are_returned()
        {
            var subject = CreateEventStore();

            await subject.AppendToStream(StreamId, 0, new EventData(new OrderCreated(StreamId)));
            await subject.AppendToStream(StreamId, 1, new EventData(new OrderDispatched(StreamId)));

            var events = await subject.ReadStreamForwards(StreamId, startPosition: 2, numberOfEventsToRead: 1);

            Assert.Equal(1, events.Count());
            Assert.IsType<OrderDispatched>(events.First().EventBody);
        }
    }
}
