using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Xunit;
using Xunit.Abstractions;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    public class SubscriptionSpikes
    {
        private readonly ITestOutputHelper output;
        private readonly DocumentClient client;

        public SubscriptionSpikes(ITestOutputHelper output)
        {
            this.output = output;

            var documentDbUri = "https://mg-eventsourcing-simple.documents.azure.com:443/";
            var authKey = "9FbXSIuFp420lalYtSsUmA9TNscZqsvseuSESRDW5saqaQxUjiv5UNGgxz2ODxKvfKIv4dKrzCVfspg97JDBTQ==";
            client = new DocumentClient(new Uri(documentDbUri), authKey);
        }

        [Fact(Skip = "Experimental spike")]
        public async void TestCatchUpSubscription()
        {
            var eventsByStream = new Dictionary<string, List<StorageEvent>>();
            var sut = new CatchUpSubscription(client, "DocumentDbEventStoreTests", 10);

            await sut.ReadEvents((checkpoint, @event) =>
            {
                output.WriteLine($"Checkpoint: {checkpoint}, Event: {@event}");

                if (!eventsByStream.ContainsKey(@event.StreamId))
                {
                    eventsByStream.Add(@event.StreamId, new List<StorageEvent>());
                }

                eventsByStream[@event.StreamId].Add(@event);
            });

            output.WriteLine($"Read {eventsByStream.Keys.Count} distinct streams");

            foreach (var streamKey in eventsByStream.Keys)
            {
                var events = eventsByStream[streamKey];
                var expectedEventNumber = 1;

                foreach (var @event in events)
                {
                    Assert.Equal(expectedEventNumber++, @event.EventNumber);
                }
            }
        }
    }
}
