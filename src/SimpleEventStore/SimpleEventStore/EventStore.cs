using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventStore
{
    public class EventStore
    {
        private readonly IStorageEngine engine;

        public EventStore(IStorageEngine engine)
        {
            this.engine = engine;
        }

        public Task AppendToStream(string streamId, int expectedVersion, params object[] events)
        {
            Guard.IsNotNullOrEmptyOrAllStream(nameof(streamId), streamId);

            var storageEvents = new List<StorageEvent>();
            var eventVersion = expectedVersion;

            for (int i = 0; i < events.Length; i++)
            {
                storageEvents.Add(new StorageEvent(streamId, events[i], ++eventVersion));
            }

            return engine.AppendToStream(streamId, storageEvents);
        }

        public Task<IEnumerable<StorageEvent>> ReadStreamForwards(string streamId)
        {
            Guard.IsNotNullOrEmpty(nameof(streamId), streamId);

            return engine.ReadStreamForwards(streamId);
        }
    }
}