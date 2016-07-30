using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventStore.Tests
{
    internal class StorageEngineFake : IStorageEngine
    {
        private readonly ConcurrentDictionary<string, List<StorageEvent>> streams = new ConcurrentDictionary<string, List<StorageEvent>>();

        public Task AppendToStream(string streamId, StorageEvent @event)
        {
            return Task.Run(() =>
            {
                if (!streams.ContainsKey(streamId))
                {
                    streams[streamId] = new List<StorageEvent>();
                }

                if (@event.EventNumber - 1 == streams[streamId].Count)
                {
                    streams[streamId].Add(@event);
                }
                else
                {
                    throw new ConcurrencyException($"Concurrency conflict when appending to stream {@streamId}. Expected revision {@event.EventNumber} : Actual revision {streams[streamId].Count}");
                }
            });
        }

        public List<StorageEvent> GetEventsForStream(string streamId)
        {
            return streams[streamId];
        }
    }
}