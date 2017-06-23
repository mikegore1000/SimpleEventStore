using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Action<IReadOnlyCollection<StorageEvent>, string> onNextEvent;
        private readonly SubscriptionOptions subscriptionOptions;
        private readonly Dictionary<string, string> checkpoints;
        private Task workerTask;

        public Subscription(DocumentClient client, Uri commitsLink, Action<IReadOnlyCollection<StorageEvent>, string> onNextEvent, string checkpoint, SubscriptionOptions subscriptionOptions)
        {
            this.client = client;
            this.commitsLink = commitsLink;
            this.onNextEvent = onNextEvent;
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
            var partitionKeyRanges = await GetPartitionKeyRanges();

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
                    var events = new List<StorageEvent>();
                    string initialCheckpointValue;

                    foreach (var @event in feedResponse)
                    {
                        events.Add(DocumentDbStorageEvent.FromDocument(@event).ToStorageEvent());
                    }

                    checkpoints.TryGetValue(pkRange.Id, out initialCheckpointValue);

                    try
                    {
                        checkpoints[pkRange.Id] = feedResponse.ResponseContinuation;
                        this.onNextEvent(events.AsReadOnly(), JsonConvert.SerializeObject(checkpoints));
                    }
                    catch(Exception)
                    {
                        if (initialCheckpointValue != null)
                        {
                            checkpoints[pkRange.Id] = initialCheckpointValue;
                        }
                        throw;
                    }
                }
            }
        }

        private async Task<IEnumerable<PartitionKeyRange>> GetPartitionKeyRanges()
        {
            var partitionKeyRanges = new List<PartitionKeyRange>();
            FeedResponse<PartitionKeyRange> pkRangesResponse;
            string continuationToken = null;

            do
            {
                pkRangesResponse = await client.ReadPartitionKeyRangeFeedAsync(commitsLink, new FeedOptions
                {
                    RequestContinuation = continuationToken
                });
                partitionKeyRanges.AddRange(pkRangesResponse);
                continuationToken = pkRangesResponse.ResponseContinuation;
            }
            while (continuationToken != null);

            return partitionKeyRanges;
        }
    }
}