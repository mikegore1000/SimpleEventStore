using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventStore.Tests
{
    internal class StorageEngineFake : IStorageEngine
    {
        private readonly Dictionary<string, List<StorageEvent>> streams = new Dictionary<string, List<StorageEvent>>();

        public Task AppendToStream(string streamId, object @event)
        {
            if (!streams.ContainsKey(streamId))
            {
                streams[streamId] = new List<StorageEvent>();
            }

            streams[streamId].Add(new StorageEvent(streamId, @event));

            return Task.FromResult(0);
        }

        public List<StorageEvent> GetEventsForStream(string streamId)
        {
            return streams[streamId];
        }
    }
}