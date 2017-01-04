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
        private int polling;

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
                PollAllEvents();
            });
        }

        private void AddEventsToAllStream(IEnumerable<StorageEvent> events)
        {
            foreach (var e in events)
            {
                allEvents.Add(e);
            }
        }

        public Task<IEnumerable<StorageEvent>> ReadStreamForwards(string streamId, int startPosition, int numberOfEventsToRead)
        {
            return Task.FromResult(streams[streamId].Skip(startPosition - 1).Take(numberOfEventsToRead));
        }

        public void SubscribeToAll(Action<StorageEvent> onNextEvent, Action<string> onCheckpoint, string checkpoint)
        {
            Guard.IsNotNull(nameof(onNextEvent), onNextEvent);
            Guard.IsNotNull(nameof(onCheckpoint), onCheckpoint);

            this.subscriptions.Add(new Subscription(onNextEvent, onCheckpoint, checkpoint));
            PollAllEvents();
        }

        private void PollAllEvents()
        {
            if (Interlocked.CompareExchange(ref polling, 1, 0) == 0)
            {
                foreach (var @event in allEvents)
                foreach (var subscription in subscriptions)
                {
                    subscription.Dispatch(@event);
                }

                Interlocked.Exchange(ref polling, 0);
            }
        }

        private class Subscription
        {
            private readonly Action<StorageEvent> onNewEvent;
            private readonly Action<string> onCheckpoint;
            private readonly string initalCheckpoint;
            private bool reachedInitialCheckpoint;

            public Subscription(Action<StorageEvent> onNewEvent, Action<string> onCheckpoint, string checkpoint)
            {
                this.onNewEvent = onNewEvent;
                this.onCheckpoint = onCheckpoint;
                this.initalCheckpoint = checkpoint;
                this.reachedInitialCheckpoint = string.IsNullOrWhiteSpace(checkpoint);
            }

            public void Dispatch(StorageEvent @event)
            {
                if (this.reachedInitialCheckpoint)
                {
                    this.onNewEvent(@event);
                    this.onCheckpoint(@event.EventId.ToString());
                }
                else
                {
                    if (@event.EventId.ToString() == initalCheckpoint)
                    {
                        this.reachedInitialCheckpoint = true;
                    }
                }
            }
        }
    }
}