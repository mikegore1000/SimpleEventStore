using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using NUnit.Framework;
using SimpleEventStore.Tests.Events;

namespace SimpleEventStore.CosmosDb.Tests
{
    [TestFixture]
    public class AzureCosmosDbEventStoreReadingPartiallyDeletedStreams
    {
        [Test]
        public async Task when_reading_a_stream_that_has_deleted_events_the_stream_can_still_be_read()
        {
            const string collectionName = "ReadingPartialStreamTests";

            var client = CosmosClientFactory.Create();
            var storageEngine = await CosmosDbStorageEngineFactory.Create(collectionName);
            var eventStore = new EventStore(storageEngine);
            var streamId = Guid.NewGuid().ToString();
            await eventStore.AppendToStream(streamId, 0, new EventData(Guid.NewGuid(), new OrderCreated(streamId)), new EventData(Guid.NewGuid(), new OrderDispatched(streamId)));
            await SimulateTimeToLiveExpiration(CosmosDbStorageEngineFactory.DefaultDatabaseName, collectionName, client, streamId);

            var stream = await eventStore.ReadStreamForwards(streamId);

            Assert.That(stream.Count, Is.EqualTo(1));
            Assert.That(stream.First().EventBody, Is.InstanceOf<OrderDispatched>());
            Assert.That(stream.First().EventNumber, Is.EqualTo(2));
        }

        private static Task SimulateTimeToLiveExpiration(string databaseName, string collectionName, CosmosClient client, string streamId)
        {
            var collection = client.GetContainer(databaseName, collectionName);
            return collection.DeleteItemAsync<dynamic>(
                $"{streamId}:1",
                new PartitionKey(streamId));
        }
    }
}
