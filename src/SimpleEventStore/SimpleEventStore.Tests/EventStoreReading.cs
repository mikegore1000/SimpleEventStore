using System;
using System.Linq;
using System.Threading.Tasks;
using SimpleEventStore.Tests.Events;
using Xunit;

namespace SimpleEventStore.Tests
{
    public abstract class EventStoreReading : EventStoreTestBase
    {
        [Fact]
        public async Task when_reading_a_stream_all_events_are_returned()
        {
            var streamId = Guid.NewGuid().ToString();
            var subject = await CreateEventStore();

            await subject.AppendToStream(streamId, 0, new EventData(Guid.NewGuid(), new OrderCreated(streamId)));
            await subject.AppendToStream(streamId, 1, new EventData(Guid.NewGuid(), new OrderDispatched(streamId)));

            var events = await subject.ReadStreamForwards(streamId);

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
            var eventStore = await CreateEventStore();
            await Assert.ThrowsAsync<ArgumentException>(async () => await eventStore.ReadStreamForwards(streamId));
        }

        [Fact]
        public async Task when_reading_a_stream_only_the_required_events_are_returned()
        {
            var streamId = Guid.NewGuid().ToString();
            var subject = await CreateEventStore();

            await subject.AppendToStream(streamId, 0, new EventData(Guid.NewGuid(), new OrderCreated(streamId)));
            await subject.AppendToStream(streamId, 1, new EventData(Guid.NewGuid(), new OrderDispatched(streamId)));

            var events = await subject.ReadStreamForwards(streamId, startPosition: 2, numberOfEventsToRead: 1);

            Assert.Equal(1, events.Count());
            Assert.IsType<OrderDispatched>(events.First().EventBody);
        }
    }
}
