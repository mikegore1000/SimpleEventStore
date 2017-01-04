using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;

namespace SimpleEventStore.AzureDocumentDb
{
    internal class Subscription
    {
        private readonly DocumentClient client;
        private readonly Uri commitsLink;
        private readonly Action<StorageEvent> onNextEvent;
        private readonly Action<string> onCheckpoint;
        private readonly SubscriptionOptions subscriptionOptions;
        private readonly Dictionary<string, string> checkpoints;
        private Task workerTask;

        public Subscription(DocumentClient client, Uri commitsLink, Action<StorageEvent> onNextEvent, Action<string> onCheckpoint, string checkpoint, SubscriptionOptions subscriptionOptions)
        {
            this.client = client;
            this.commitsLink = commitsLink;
            this.onNextEvent = onNextEvent;
            this.onCheckpoint = onCheckpoint;
            this.checkpoints = checkpoint == null ? new Dictionary<string, string>() : JsonConvert.DeserializeObject<Dictionary<string, string>>(checkpoint);
            this.subscriptionOptions = subscriptionOptions;
        }

        // TODO: Configure the retry policy, also allow the subscription to be canclled (use a CancellationToken)
        public void Start()
        {
            workerTask = Task.Run(async () =>
            {
                while (true)
                {
                    await ReadEvents();
                    await Task.Delay(subscriptionOptions.PollEvery);
                }
            });
        }

        private async Task ReadEvents()
        {
            var partitionKeyRanges = new List<PartitionKeyRange>();
            FeedResponse<PartitionKeyRange> pkRangesResponse;

            do
            {
                pkRangesResponse = await client.ReadPartitionKeyRangeFeedAsync(commitsLink);
                partitionKeyRanges.AddRange(pkRangesResponse);
            }
            while (pkRangesResponse.ResponseContinuation != null);

            foreach (var pkRange in partitionKeyRanges)
            {
                string continuation;
                checkpoints.TryGetValue(pkRange.Id, out continuation);

                IDocumentQuery<Document> query = client.CreateDocumentChangeFeedQuery(
                    commitsLink,
                    new ChangeFeedOptions
                    {
                        PartitionKeyRangeId = pkRange.Id,
                        StartFromBeginning = true,
                        RequestContinuation = continuation,
                        MaxItemCount = subscriptionOptions.MaxItemCount
                    });

                while (query.HasMoreResults)
                {
                    var feedResponse = await query.ExecuteNextAsync<Document>();

                    foreach (var @event in feedResponse)
                    {
                        this.onNextEvent(DocumentDbStorageEvent.FromDocument(@event).ToStorageEvent());
                    }

                    checkpoints[pkRange.Id] = feedResponse.ResponseContinuation;
                    this.onCheckpoint(JsonConvert.SerializeObject(checkpoints));
                }
            }
        }
    }
}