using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleEventStore
{
    public class InMemoryStorageEngine : IStorageEngine
    {
        private const string AllStreamId = "$all";
        private readonly ConcurrentDictionary<string, List<StorageEvent>> streams = new ConcurrentDictionary<string, List<StorageEvent>>();

        public InMemoryStorageEngine()
        {
            streams[AllStreamId] = new List<StorageEvent>();
        }

        public Task AppendToStream(string streamId, IEnumerable<StorageEvent> events)
        {
            return Task.Run(() =>
            {
                if (!streams.ContainsKey(streamId))
                {
                    streams[streamId] = new List<StorageEvent>();
                }

                var firstEvent = events.First();

                if (firstEvent.EventNumber - 1 == streams[streamId].Count)
                {
                    streams[streamId].AddRange(events);
                    streams[AllStreamId].AddRange(events);
                }
                else
                {
                    throw new ConcurrencyException($"Concurrency conflict when appending to stream {@streamId}. Expected revision {firstEvent.EventNumber} : Actual revision {streams[streamId].Count}");
                }
            });
        }

        public Task<IEnumerable<StorageEvent>> ReadStreamForwards(string streamId, int startPosition, int numberOfEventsToRead)
        {
            return Task.FromResult(streams[streamId].Skip(startPosition - 1).Take(numberOfEventsToRead));
        }
    }
}