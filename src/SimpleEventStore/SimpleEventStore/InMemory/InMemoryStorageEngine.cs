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
        private int polling;
        private List<Action<string, StorageEvent>> subscriptions = new List<Action<string, StorageEvent>>();

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

        public void SubscribeToAll(Action<string, StorageEvent> onNextEvent)
        {
            Guard.IsNotNull(nameof(onNextEvent), onNextEvent);

            this.subscriptions.Add(onNextEvent);
            PollAllEvents();
        }

        private void PollAllEvents()
        {
            if (Interlocked.CompareExchange(ref polling, 1, 0) == 0)
            {
                foreach (var @event in allEvents)
                foreach (var subscription in subscriptions)
                {
                    subscription?.Invoke(string.Empty, @event);
                }

                Interlocked.Exchange(ref polling, 0);
            }
        }
    }
}