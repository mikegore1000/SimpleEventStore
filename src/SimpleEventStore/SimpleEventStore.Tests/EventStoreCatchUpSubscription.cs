using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimpleEventStore.Tests.Events;
using Xunit;

namespace SimpleEventStore.Tests
{
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

        // TODO: Missing tests
        // 1. Support multiple subscriptions
        // 3. Consider threading across all tests
    }
}
