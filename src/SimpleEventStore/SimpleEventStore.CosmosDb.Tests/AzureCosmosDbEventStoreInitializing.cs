using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using NUnit.Framework;

namespace SimpleEventStore.CosmosDb.Tests
{
    [TestFixture]
    public class AzureCosmosV3EventStoreInitializing
    {
        private const string DatabaseName = "EventStoreTests-Initialize-CosmosV3";
        private readonly CosmosClient client = CosmosClientFactory.Create();

        [TearDown]
        public Task TearDownDatabase()
        {
            return client.GetDatabase(DatabaseName).DeleteAsync();
        }

        [Test]
        public async Task when_initializing_all_expected_resources_are_created()
        {
            var collectionName = "AllExpectedResourcesAreCreated_" + Guid.NewGuid();
            var storageEngine = await InitialiseStorageEngine(collectionName, collectionThroughput: TestConstants.RequestUnits);

            var database = client.GetDatabase(DatabaseName);
            var collection = database.GetContainer(collectionName);

            const string queryText = "SELECT * FROM s";

            var storedProcedures = await collection.Scripts.GetStoredProcedureQueryIterator<StoredProcedureProperties>(
                queryText).ReadNextAsync();

            var expectedStoredProcedure = storedProcedures.Resource.FirstOrDefault(s => s.Id.StartsWith("appendToStream-"));

            var collectionResponse = await collection.ReadContainerAsync();
            var collectionProperties = collectionResponse.Resource;

            var offer = await collection.ReadThroughputAsync(requestOptions: null);

            Assert.That(expectedStoredProcedure, Is.Not.Null);
            Assert.That(offer.Resource.AutoscaleMaxThroughput, Is.EqualTo(TestConstants.RequestUnits));
            Assert.That(collectionProperties.DefaultTimeToLive, Is.Null);
            Assert.That(collectionProperties.PartitionKeyPath, Is.EqualTo("/streamId"));
            Assert.That(collectionProperties.IndexingPolicy.IncludedPaths.Count, Is.EqualTo(1));
            Assert.That(collectionProperties.IndexingPolicy.IncludedPaths[0].Path, Is.EqualTo("/*"));
            Assert.That(collectionProperties.IndexingPolicy.ExcludedPaths.Count, Is.EqualTo(3));
            Assert.That(collectionProperties.IndexingPolicy.ExcludedPaths[0].Path, Is.EqualTo("/body/*"));
            Assert.That(collectionProperties.IndexingPolicy.ExcludedPaths[1].Path, Is.EqualTo("/metadata/*"));
        }

        [Test]
        public async Task when_using_shared_throughput_it_is_set_at_a_database_level()
        {
            const int dbThroughput = 8000;
            var collectionName = "SharedCollection_" + Guid.NewGuid();

            await InitialiseStorageEngine(collectionName, dbThroughput: dbThroughput);

            Assert.AreEqual(dbThroughput, await GetDatabaseThroughput());
            Assert.AreEqual(null, await GetCollectionThroughput(collectionName));
        }

        [TestCase(60)]
        [TestCase(10)]
        [TestCase(90)]
        public async Task when_initializing_with_a_time_to_live_it_is_set(int ttl)
        {
            var collectionName = "TimeToLiveIsSet_" + Guid.NewGuid();
            var storageEngine = await CosmosDbStorageEngineFactory.Create(collectionName, DatabaseName,
                x =>
                {
                    x.UseCollection(o => o.DefaultTimeToLive = ttl);
                });

            var collection = await client.GetContainer(DatabaseName, collectionName).ReadContainerAsync();

            var collectionProperties = collection.Resource;

            Assert.That(collectionProperties.DefaultTimeToLive, Is.EqualTo(ttl));
        }

        [Test]
        public async Task when_throughput_is_set_offer_is_updated()
        {
            var dbThroughput = 8000;
            var collectionThroughput = 4000;
            var collectionName = "UpdateThroughput_" + Guid.NewGuid();

            await InitialiseStorageEngine(collectionName, collectionThroughput, dbThroughput);

            Assert.AreEqual(dbThroughput, await GetDatabaseThroughput());
            Assert.AreEqual(collectionThroughput, await GetCollectionThroughput(collectionName));

            var collectionThroughput2 = 5000;

            await InitialiseStorageEngine(collectionName, collectionThroughput2, dbThroughput);

            Assert.AreEqual(dbThroughput, await GetDatabaseThroughput());
            Assert.AreEqual(collectionThroughput2, await GetCollectionThroughput(collectionName));
        }

        [TestCase(null, null, null, null)]
        [TestCase(6000, null, 6000, null)]
        [TestCase(null, 6000, null, 6000)]
        [TestCase(6000, 6000, 6000, 6000)]
        [TestCase(6000, 10000, 6000, 10000)]
        [TestCase(10000, 6000, 10000, 6000)]
        public async Task set_database_and_collection_throughput_when_database_has_not_been_created(int? dbThroughput, int? collectionThroughput, int? expectedDbThroughput, int? expectedCollectionThroughput)
        {
            var collectionName = "CollectionThroughput_" + Guid.NewGuid();

            await InitialiseStorageEngine(collectionName, collectionThroughput, dbThroughput);

            Assert.AreEqual(expectedDbThroughput, await GetDatabaseThroughput());
            Assert.AreEqual(expectedCollectionThroughput, await GetCollectionThroughput(collectionName));
        }


        [TestCase(null, 5000, null)]
        [TestCase(10000, 5000, 10000)]
        public async Task set_database_and_collection_throughput_when_database_has_already_been_created(int? collectionThroughput, int? expectedDbThroughput, int? expectedCollectionThroughput)
        {
            const int existingDbThroughput = 5000;
            await CreateDatabase(existingDbThroughput);
            var collectionName = "CollectionThroughput_" + Guid.NewGuid();

            await InitialiseStorageEngine(collectionName, collectionThroughput, null);

            Assert.AreEqual(expectedDbThroughput, await GetDatabaseThroughput());
            Assert.AreEqual(expectedCollectionThroughput, await GetCollectionThroughput(collectionName));
        }

        private static async Task<IStorageEngine> InitialiseStorageEngine(string collectionName, int? collectionThroughput = null, int? dbThroughput = null)
        {
            var storageEngine = await CosmosDbStorageEngineFactory.Create(collectionName, DatabaseName, x =>
            {
                x.UseCollection(o => o.CollectionRequestUnits = collectionThroughput);
                x.UseDatabase(o => o.DatabaseRequestUnits = dbThroughput);
            });

            return await storageEngine.Initialise();
        }

        public async Task<int?> GetCollectionThroughput(string collectionName)
        {
            try
            {
                var collection = client.GetContainer(DatabaseName, collectionName);
                var autoscaleContainerThroughput = await collection.ReadThroughputAsync(requestOptions: null);
                return autoscaleContainerThroughput.Resource.AutoscaleMaxThroughput;
            }
            catch (CosmosException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<int?> GetDatabaseThroughput()
        {
            try
            {
                var database = client.GetDatabase(DatabaseName);
                var autoscaleContainerThroughput = await database.ReadThroughputAsync(requestOptions: null);
                return autoscaleContainerThroughput.Resource.AutoscaleMaxThroughput;
            }
            catch (CosmosException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        private Task CreateDatabase(int databaseRequestUnits)
        {
            return client.CreateDatabaseIfNotExistsAsync(DatabaseName, ThroughputProperties.CreateAutoscaleThroughput(databaseRequestUnits));
        }

    }
}