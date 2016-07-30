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

        public Task AppendToStream(string streamId, object @event)
        {
            engine.AppendToStream(streamId, @event);
            return Task.FromResult(0);
        }
    }
}