using System.Collections;
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

        public Task AppendToStream(string streamId, object @event, int expectedVersion)
        {
            return engine.AppendToStream(streamId, new StorageEvent(streamId, @event, expectedVersion + 1));
        }

        public Task<IEnumerable<StorageEvent>> ReadStreamForwards(string streamId)
        {
            return engine.ReadStreamForwards(streamId);
        }
    }
}