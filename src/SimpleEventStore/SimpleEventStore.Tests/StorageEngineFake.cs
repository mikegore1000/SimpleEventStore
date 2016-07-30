using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleEventStore.Tests
{
    internal class StorageEngineFake : IStorageEngine
    {
        private readonly ConcurrentDictionary<string, List<StorageEvent>> streams = new ConcurrentDictionary<string, List<StorageEvent>>();

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
                }
                else
                {
                    throw new ConcurrencyException($"Concurrency conflict when appending to stream {@streamId}. Expected revision {firstEvent.EventNumber} : Actual revision {streams[streamId].Count}");
                }
            });
        }

        public Task<IEnumerable<StorageEvent>> ReadStreamForwards(string streamId)
        {
            return Task.FromResult<IEnumerable<StorageEvent>>(streams[streamId]);
        }
    }
}