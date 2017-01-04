using Microsoft.Azure.Documents;

namespace SimpleEventStore.AzureDocumentDb
{
    public class DatabaseOptions
    {
        public DatabaseOptions(ConsistencyLevel consistencyLevel, int collectionRequestUnits)
        {
            this.ConsistencyLevel = consistencyLevel;
            this.CollectionRequestUnits = collectionRequestUnits;
        }

        public ConsistencyLevel ConsistencyLevel { get; }

        public int CollectionRequestUnits { get; }
    }
}