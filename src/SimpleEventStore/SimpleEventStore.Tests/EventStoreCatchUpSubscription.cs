using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SimpleEventStore.Tests.Events;
using Xunit;

namespace SimpleEventStore.Tests
{
    public abstract class EventStoreCatchUpSubscription : EventStoreTestBase
    {
        private const int NumberOfStreamsToCreate = 10;

        [Fact]
        public async void when_a_subscription_is_started_with_no_checkpoint_token_all_stored_events_are_read_in_stream_order()
        {
            var sut = await CreateEventStore();
            var streams = new Dictionary<string, Queue<EventData>>();
            var completionSource = new TaskCompletionSource<object>();

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

                    if (streams.Count == 0)
                    {
                        completionSource.SetResult(null);
                    }
                }
            });

            await completionSource.Task;

            Assert.Equal(0, streams.Count);
        }

        [Fact]
        public async void when_a_subscription_is_started_with_no_checkpoint_token_new_events_written_are_read_in_stream_order()
        {
            var sut = await CreateEventStore();
            var streams = new Dictionary<string, Queue<EventData>>();
            var completionSource = new TaskCompletionSource<object>();

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

                    if (streams.Count == 0)
                    {
                        completionSource.SetResult(null);
                    }
                }
            });

            await CreateStreams(streams, sut);
            await completionSource.Task;

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
            var subscription1Called = new TaskCompletionSource<bool>(false);
            var subscription2Called = new TaskCompletionSource<bool>(false);

            var sut = await CreateEventStore();
            sut.SubscribeToAll((c, e) =>
            {
                if (!subscription1Called.Task.IsCompleted)
                {
                    subscription1Called.SetResult(true);
                }
            });
            sut.SubscribeToAll((c, e) =>
            {
                if (!subscription2Called.Task.IsCompleted)
                {
                    subscription2Called.SetResult(true);
                }
            });

            var streamId = Guid.NewGuid().ToString();
            await sut.AppendToStream(streamId, 0, new EventData(Guid.NewGuid(), new OrderCreated(streamId)));

            Task.WaitAll(subscription1Called.Task, subscription2Called.Task);
            Assert.True(subscription1Called.Task.Result);
            Assert.True(subscription2Called.Task.Result);
        }

        [Fact]
        public async Task when_a_subscription_is_started_with_a_checkpoint_only_events_after_the_checkpoint_are_received()
        {
            var initialCheckpointObtained = new TaskCompletionSource<string>();
            var resumedEventRead = new TaskCompletionSource<StorageEvent>();
            var streamId = Guid.NewGuid().ToString();
            var sut = await CreateEventStore();
            var orderCreatedId = Guid.NewGuid();

            await sut.AppendToStream(
                streamId,
                0,
                new EventData(orderCreatedId, new OrderCreated(streamId))
            );

            await sut.AppendToStream(
                streamId,
                1,
                new EventData(Guid.NewGuid(), new OrderDispatched(streamId))
            );

            sut.SubscribeToAll((c, e) =>
            {
                if (!initialCheckpointObtained.Task.IsCompleted && e.EventId == orderCreatedId)
                {
                    initialCheckpointObtained.SetResult(c);
                }
            });

            await initialCheckpointObtained.Task;
            var checkpoint = initialCheckpointObtained.Task.Result;

            sut.SubscribeToAll((c, e) =>
            {
                if (!resumedEventRead.Task.IsCompleted && e.StreamId == streamId)
                {
                    resumedEventRead.SetResult(e);
                }
            }, checkpoint);

            await resumedEventRead.Task;

            Assert.NotNull(resumedEventRead.Task.Result);
            Assert.IsType<OrderDispatched>(resumedEventRead.Task.Result.EventBody);
        }

        private static async Task CreateStreams(Dictionary<string, Queue<EventData>> streams, EventStore sut)
        {
            var streamsToCommit = new Dictionary<string, EventData[]>();

            for (int i = 0; i < NumberOfStreamsToCreate; i++)
            {
                var streamId = Guid.NewGuid().ToString();
                var createdEvent = new EventData(Guid.NewGuid(), new OrderCreated(streamId), null);
                var dispatchedEvent = new EventData(Guid.NewGuid(), new OrderDispatched(streamId), null);
                var streamOrder = new Queue<EventData>();

                streamOrder.Enqueue(createdEvent);
                streamOrder.Enqueue(dispatchedEvent);

                streams.Add(streamId, streamOrder);

                streamsToCommit.Add(streamId, new [] { createdEvent, dispatchedEvent });
            }

            foreach (var stream in streamsToCommit)
            {
                await sut.AppendToStream(stream.Key, 0, stream.Value);
            }
        }
    }
}
