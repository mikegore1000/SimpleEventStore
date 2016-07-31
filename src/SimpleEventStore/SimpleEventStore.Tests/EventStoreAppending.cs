using System;
using System.Linq;
using System.Threading.Tasks;
using SimpleEventStore.Tests.Events;
using SimpleEventStore.Tests.Metadata;
using Xunit;

namespace SimpleEventStore.Tests
{
    public abstract class EventStoreAppending : EventStoreTestBase
    {
        private const string StreamId = "TEST-ORDER";

        [Fact]
        public async Task when_appending_to_a_new_stream_the_event_is_saved()
        {
            var subject = CreateEventStore();
            var @event = new EventData(new OrderCreated(StreamId));

            await subject.AppendToStream(StreamId, 0, @event);

            var stream = await subject.ReadStreamForwards(StreamId);
            Assert.Equal(1, stream.Count());
            Assert.Equal(StreamId, stream.Single().StreamId);
            Assert.Equal(@event.Body, stream.Single().EventBody);
            Assert.Equal(1, stream.Single().EventNumber);
        }

        [Fact]
        public async Task when_appending_to_an_existing_stream_the_event_is_saved()
        {
            var subject = CreateEventStore();
            await subject.AppendToStream(StreamId, 0, new EventData(new OrderCreated(StreamId)));
            var @event = new EventData(new OrderDispatched(StreamId));

            await subject.AppendToStream(StreamId, 1, @event);

            var stream = await subject.ReadStreamForwards(StreamId);
            Assert.Equal(2, stream.Count());
            Assert.Equal(@event.Body, stream.Skip(1).Single().EventBody);
            Assert.Equal(2, stream.Skip(1).Single().EventNumber);
        }

        [InlineData(-1)]
        [InlineData(1)]
        public async Task when_appending_to_a_new_stream_with_an_unexpected_version__a_concurrency_error_is_thrown(int expectedVersion)
        {
            var subject = CreateEventStore();
            var @event = new EventData(new OrderDispatched(StreamId));

            await Assert.ThrowsAsync<ConcurrencyException>(async () => await subject.AppendToStream(StreamId, expectedVersion, @event));
        }

        [InlineData(0)]
        [InlineData(2)]
        public async Task when_appending_to_an_existing_stream_with_an_unexpected_version_a_concurrency_error_is_thrown(int expectedVersion)
        {
            var subject = CreateEventStore();
            await subject.AppendToStream(StreamId, 0, new EventData(new OrderCreated(StreamId)));

            var @event = new EventData(new OrderDispatched(StreamId));

            await Assert.ThrowsAsync<ConcurrencyException>(async () => await subject.AppendToStream(StreamId, expectedVersion, @event));
        }

        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("$all")]
        public async Task when_appending_to_an_invalid_stream_id_an_argument_error_is_thrown(string streamId)
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => await CreateEventStore().AppendToStream(streamId, 0, new EventData(new OrderCreated(streamId))));
        }

        [Fact]
        public async Task when_appending_to_a_new_stream_with_multiple_events_then_they_are_saved()
        {
            var subject = CreateEventStore();
            var @events = new []
            {
                new EventData(new OrderCreated(StreamId)),
                new EventData(new OrderDispatched(StreamId))
            };

            await subject.AppendToStream(StreamId, 0, @events);

            var savedEvents = await subject.ReadStreamForwards(StreamId);

            Assert.Equal(2, savedEvents.Count());
            Assert.Equal(StreamId, savedEvents.First().StreamId);
            Assert.Equal(1, savedEvents.First().EventNumber);
            Assert.Equal(StreamId, savedEvents.Skip(1).Single().StreamId);
            Assert.Equal(2, savedEvents.Skip(1).Single().EventNumber);
        }

        [Fact]
        public async Task when_appending_to_a_new_stream_the_event_metadata_is_saved()
        {
            var subject = CreateEventStore();
            var metadata = new TestMetadata { Value = "Hello" };
            var @event = new EventData(new OrderCreated(StreamId), metadata);

            await subject.AppendToStream(StreamId, 0, @event);

            var stream = await subject.ReadStreamForwards(StreamId);
            Assert.Equal(metadata, stream.Single().Metadata);
        }
    }
}
