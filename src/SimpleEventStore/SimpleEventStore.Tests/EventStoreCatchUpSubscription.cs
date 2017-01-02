using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimpleEventStore.Tests.Events;
using Xunit;

namespace SimpleEventStore.Tests
{
    // TODO: Consider threading across all tests - will likely fail with any subscription impl that requires a background thread
    public abstract class EventStoreCatchUpSubscription : EventStoreTestBase
    {
        private const int NumberOfStreamsToCreate = 100;

        [Fact]
        public async void when_a_subscription_is_started_with_no_checkpoint_token_all_stored_events_are_read_in_stream_order()
        {
            var sut = await CreateEventStore();
            var streams = new Dictionary<string, Queue<EventData>>();

            await CreateStreams(streams, sut);

            sut.SubscribeToAll((checkpoint, @event) =>
            {
                if (streams.ContainsKey(@event.StreamId))
                {
                    var stream = streams[@event.StreamId];

                    Assert.Equal(stream.Peek().EventId, @event.EventId);
                    stream.Dequeue();

                    if (stream.Count == 0)
                    {
                        streams.Remove(@event.StreamId);
                    }
                }
            });

            Assert.Equal(0, streams.Count);
        }

        [Fact]
        public async void when_a_subscription_is_started_with_no_checkpoint_token_new_events_written_are_read_in_stream_order()
        {
            var sut = await CreateEventStore();
            var streams = new Dictionary<string, Queue<EventData>>();

            sut.SubscribeToAll((checkpoint, @event) =>
            {
                if (streams.ContainsKey(@event.StreamId))
                {
                    var stream = streams[@event.StreamId];

                    Assert.Equal(stream.Peek().EventId, @event.EventId);
                    stream.Dequeue();

                    if (stream.Count == 0)
                    {
                        streams.Remove(@event.StreamId);
                    }
                }
            });

            await CreateStreams(streams, sut);

            Assert.Equal(0, streams.Count);
        }

        [Fact]
        public async Task when_a_subscription_is_started_a_next_event_function_must_be_supplied()
        {
            var sut = await CreateEventStore();
            Assert.Throws<ArgumentNullException>(() => sut.SubscribeToAll(null));
        }

        [Fact]
        public async Task when_multiple_subscriptions_are_created_they_all_receive_events()
        {
            var subscription1Called = false;
            var subscription2Called = false;
            var sut = await CreateEventStore();
            sut.SubscribeToAll((c, e) => subscription1Called = true);
            sut.SubscribeToAll((c, e) => subscription2Called = true);

            var streamId = Guid.NewGuid().ToString();
            await sut.AppendToStream(streamId, 0, new EventData(Guid.NewGuid(), new OrderCreated(streamId)));

            Assert.True(subscription1Called);
            Assert.True(subscription2Called);
        }

        [Fact]
        public async Task when_a_subscription_is_started_with_a_checkpoint_only_events_newer_than_the_checkpoint_are_received()
        {
            string checkpoint = null;
            StorageEvent receivedEvent = null;
            var streamId = Guid.NewGuid().ToString();
            var sut = await CreateEventStore();
            
            await sut.AppendToStream(streamId, 0, new EventData(Guid.NewGuid(), new OrderCreated(streamId)));
            sut.SubscribeToAll((c, e) =>
            {
                if (checkpoint == null)
                {
                    checkpoint = c;
                }
            });

            await sut.AppendToStream(streamId, 1, new EventData(Guid.NewGuid(), new OrderDispatched(streamId)));

            sut.SubscribeToAll((c, e) =>
            {
                if (receivedEvent == null)
                {
                    receivedEvent = e;
                }
            }, checkpoint);

            Assert.NotNull(receivedEvent);
            Assert.IsType<OrderDispatched>(receivedEvent.EventBody);
        }

        private static async Task CreateStreams(Dictionary<string, Queue<EventData>> streams, EventStore sut)
        {
            for (int i = 0; i < NumberOfStreamsToCreate; i++)
            {
                var streamId = Guid.NewGuid().ToString();
                var createdEvent = new EventData(Guid.NewGuid(), new OrderCreated(streamId), null);
                var dispatchedEvent = new EventData(Guid.NewGuid(), new OrderDispatched(streamId), null);
                var streamOrder = new Queue<EventData>();

                streamOrder.Enqueue(createdEvent);
                streamOrder.Enqueue(dispatchedEvent);

                streams.Add(streamId, streamOrder);

                await sut.AppendToStream(streamId, 0, createdEvent, dispatchedEvent);
            }
        }
    }
}
