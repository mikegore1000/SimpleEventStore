using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimpleEventStore.Tests.Events;
using Xunit;

namespace SimpleEventStore.Tests
{
    public abstract class EventStoreCatchUpSubscription : EventStoreTestBase
    {
        private const int NumberOfStreamsToCreate = 10;

        [Fact]
        public async Task when_a_subscription_is_started_with_no_checkpoint_token_all_stored_events_are_read_in_stream_order()
        {
            var sut = await GetEventStore();
            var streams = new Dictionary<string, Queue<EventData>>();
            var completionSource = new TaskCompletionSource<object>();

            await CreateStreams(streams, sut);

            sut.SubscribeToAll(
                (events, checkpoint) =>
                {
                    foreach (var @event in events)
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
                    }
                });

            await completionSource.Task;

            Assert.Equal(0, streams.Count);
        }

        [Fact]
        public async Task when_a_subscription_is_started_with_no_checkpoint_token_new_events_written_are_read_in_stream_order()
        {
            var sut = await GetEventStore();
            var streams = new Dictionary<string, Queue<EventData>>();
            var completionSource = new TaskCompletionSource<object>();

            sut.SubscribeToAll(
                (events, checkpoint) =>
                {
                    foreach (var @event in events)
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
                    }
                });

            await CreateStreams(streams, sut);
            await completionSource.Task;

            Assert.Equal(0, streams.Count);
        }

        [Fact]
        public async Task when_a_subscription_is_started_a_next_event_function_must_be_supplied()
        {
            var sut = await GetEventStore();
            Assert.Throws<ArgumentNullException>(() => sut.SubscribeToAll(null));
        }

        [Fact]
        public async Task when_multiple_subscriptions_are_created_they_receive_events()
        {
            var subscription1Called = new TaskCompletionSource<bool>(false);
            var subscription2Called = new TaskCompletionSource<bool>(false);

            var sut = await GetEventStore();
            sut.SubscribeToAll(
                (events, checkpoint) =>
                {
                    if (!subscription1Called.Task.IsCompleted)
                    {
                        subscription1Called.SetResult(true);
                    }
                });
            sut.SubscribeToAll(
                (events, checkpoint) =>
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
            var sut = await GetEventStore();
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

            sut.SubscribeToAll(
                (events, c) =>
                {
                    foreach (var e in events)
                    {
                        if (e.EventId == orderCreatedId)
                        {
                            initialCheckpointObtained.SetResult(c);
                        }
                    }
                });

            await initialCheckpointObtained.Task;
            var checkpoint = initialCheckpointObtained.Task.Result;

            sut.SubscribeToAll(
                (events, c) =>
                {
                    foreach (var e in events)
                    {
                        if (!resumedEventRead.Task.IsCompleted && e.StreamId == streamId)
                        {
                            resumedEventRead.SetResult(e);
                        }
                    }
                },
                null,
                checkpoint);

            await resumedEventRead.Task;

            Assert.NotNull(resumedEventRead.Task.Result);
            Assert.IsType<OrderDispatched>(resumedEventRead.Task.Result.EventBody);
        }

        [Fact]
        public async Task when_a_subscription_is_started_it_can_be_stopped_and_no_more_events_are_processed()
        {
            var eventStore = await GetEventStore();
            var callbackCompletionSource = new TaskCompletionSource<Exception>();

            var subscription = eventStore.SubscribeToAll((events, c) => {  }, (sub, exception) => callbackCompletionSource.SetResult(exception));
            subscription.Stop();

            callbackCompletionSource.Task.Wait(TimeSpan.FromSeconds(5));
            Assert.Null(callbackCompletionSource.Task.Result);
        }

        [Fact]
        public async Task when_a_subscription_throws_an_exception_it_is_stopped_and_the_exception_is_supplied()
        {
            var streamId = Guid.NewGuid().ToString();
            var eventStore = await GetEventStore();
            var callbackCompletionSource = new TaskCompletionSource<Exception>();
            await eventStore.AppendToStream(streamId, 0, new EventData(Guid.NewGuid(), new OrderCreated(streamId)));

            eventStore.SubscribeToAll((events, c) => throw new Exception("TEST"), (sub, exception) => callbackCompletionSource.SetResult(exception));

            callbackCompletionSource.Task.Wait(TimeSpan.FromSeconds(5));
            Assert.NotNull(callbackCompletionSource.Task.Result);
        }

        // TODO: Add test to prove a subscription can be restarted...
        // TODO: Add test to prove starting a running subscription has no side effects...
        // TODO: Add test to prove stopping a stopped subscription has no side effects...

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
