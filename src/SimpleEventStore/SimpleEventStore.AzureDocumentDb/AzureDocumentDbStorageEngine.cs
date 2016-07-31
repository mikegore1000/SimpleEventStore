using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventStore.AzureDocumentDb
{
    public class AzureDocumentDbStorageEngine : IStorageEngine
    {
        public Task AppendToStream(string streamId, IEnumerable<StorageEvent> events)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<StorageEvent>> ReadStreamForwards(string streamId, int startPosition, int numberOfEventsToRead)
        {
            throw new System.NotImplementedException();
        }
    }
}
