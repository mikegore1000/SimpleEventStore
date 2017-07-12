using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventStore.InMemory
{
    public class InMemoryStorageEngine : IStorageEngine
    {
        private readonly ConcurrentDictionary<string, List<StorageEvent>> streams = new ConcurrentDictionary<string, List<StorageEvent>>();
        private readonly List<StorageEvent> allEvents = new List<StorageEvent>();
        private readonly List<Subscription> subscriptions = new List<Subscription>();

        public Task AppendToStream(string streamId, IEnumerable<StorageEvent> events)
        {
            return Task.Run(() =>
            {
                if (!streams.ContainsKey(streamId))
                {
                    streams[streamId] = new List<StorageEvent>();
                }

                var firstEvent = events.First();

                if (firstEvent.EventNumber - 1 != streams[streamId].Count)
                {
                    throw new ConcurrencyException($"Concurrency conflict when appending to stream {@streamId}. Expected revision {firstEvent.EventNumber} : Actual revision {streams[streamId].Count}");
                }

                streams[streamId].AddRange(events);
                AddEventsToAllStream(events);
            });
        }

        private void AddEventsToAllStream(IEnumerable<StorageEvent> events)
        {
            foreach (var e in events)
            {
                allEvents.Add(e);
            }
        }

        public Task<IReadOnlyCollection<StorageEvent>> ReadStreamForwards(string streamId, int startPosition, int numberOfEventsToRead)
        {
            IReadOnlyCollection<StorageEvent> result = streams[streamId].Skip(startPosition - 1).Take(numberOfEventsToRead).ToList().AsReadOnly();
            return Task.FromResult(result);
        }

        public ISubscription SubscribeToAll(Action<IReadOnlyCollection<StorageEvent>, string> onNextEvent, Action<ISubscription, Exception> onStopped, string checkpoint)
        {
            Guard.IsNotNull(nameof(onNextEvent), onNextEvent);

            var subscription = new Subscription(this.allEvents, onNextEvent, onStopped, checkpoint);
            this.subscriptions.Add(subscription);

            subscription.Start();
            return subscription;
        }

        public Task<IStorageEngine> Initialise()
        {
            return Task.FromResult<IStorageEngine>(this);
        }

        public class Subscription : ISubscription
        {
            private readonly IEnumerable<StorageEvent> allStream;
            private readonly Action<IReadOnlyCollection<StorageEvent>, string> onNextEvent;
            private readonly Action<ISubscription, Exception> onStopped;
            private readonly string initialCheckpoint;
            private int currentPosition;
            private Task workerTask;
            private CancellationTokenSource cancellationSource;

            public Subscription(IEnumerable<StorageEvent> allStream, Action<IReadOnlyCollection<StorageEvent>, string> onNextEvent, Action<ISubscription, Exception> onStopped, string checkpoint)
            {
                this.allStream = allStream;
                this.onNextEvent = onNextEvent;
                this.onStopped = onStopped;
                this.initialCheckpoint = checkpoint;
            }

            public void Start()
            {
                cancellationSource = new CancellationTokenSource();

                workerTask = Task.Run(async () =>
                {
                    try
                    {
                        while (!cancellationSource.Token.IsCancellationRequested)
                        {
                            ReadEvents();
                            await Task.Delay(500);
                        }
                    }
                    catch (Exception e)
                    {
                        this.onStopped?.Invoke(this, e);
                    }

                    this.onStopped?.Invoke(this, null);
                }, cancellationSource.Token);
            }

            public void Stop()
            {
                this.cancellationSource.Cancel();
            }

            private void ReadEvents()
            {
                var snapshot = allStream.Skip(this.currentPosition).ToList();

                foreach (var @event in snapshot)
                {
                    bool dispatchEvents = true;

                    if (this.initialCheckpoint == null || this.initialCheckpoint == @event.EventId.ToString())
                    {
                        dispatchEvents = this.initialCheckpoint == null;
                    }

                    if(dispatchEvents)
                    {
                        this.onNextEvent(new[] { @event }, @event.EventId.ToString());
                        this.currentPosition++;
                    }
                }
            }
        }
    }
}