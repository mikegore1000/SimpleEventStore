using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using SimpleEventStore.Tests;
using Xunit;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    public class AzureDocumentDbEventStoreCatchUpSubscription : EventStoreCatchUpSubscription
    {
        protected override Task<IStorageEngine> CreateStorageEngine()
        {
            return StorageEngineFactory.Create("CatchUpSubscriptionTests");
        }

        [Fact]
        public void when_subscription_options_have_not_been_supplied_the_subscription_feature_cannot_be_used()
        {
            var client = new DocumentClient(new Uri("https://localhost:8081/"), "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
            var sut = new AzureDocumentDbStorageEngineBuilder(client, "Test").Build();

            Assert.Throws<SubscriptionsNotConfiguredException>(() => sut.SubscribeToAll((e, c) => { }, null, null));
        }
    }
}
