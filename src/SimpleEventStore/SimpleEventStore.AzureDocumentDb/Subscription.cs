using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;

namespace SimpleEventStore.AzureDocumentDb
{
    public class Subscription : ISubscription
    {
        private readonly DocumentClient client;
        private readonly Uri commitsLink;
        private readonly Action<IReadOnlyCollection<StorageEvent>, string> onNextEvent;
        private Action<ISubscription, Exception> onStopped;
        private readonly SubscriptionOptions subscriptionOptions;
        private readonly Dictionary<string, string> checkpoints;
        private readonly LoggingOptions loggingOptions;
        private readonly ISerializationTypeMap serializationTypeMap;
        private Task workerTask;
        private CancellationTokenSource cancellationSource;

        public Subscription(DocumentClient client, Uri commitsLink, Action<IReadOnlyCollection<StorageEvent>, string> onNextEvent, Action<ISubscription, Exception> onStopped, string checkpoint, SubscriptionOptions subscriptionOptions, LoggingOptions loggingOptions, ISerializationTypeMap serializationTypeMap)
        {
            this.client = client;
            this.commitsLink = commitsLink;
            this.onNextEvent = onNextEvent;
            this.onStopped = onStopped;
            this.checkpoints = checkpoint == null ? new Dictionary<string, string>() : JsonConvert.DeserializeObject<Dictionary<string, string>>(checkpoint);
            this.subscriptionOptions = subscriptionOptions;
            this.loggingOptions = loggingOptions;
            this.serializationTypeMap = serializationTypeMap;
        }

        public void Start()
        {
            cancellationSource = new CancellationTokenSource();

            workerTask = Task.Run(async () =>
            {
                try
                {
                    while (!cancellationSource.Token.IsCancellationRequested)
                    {
                        await ReadEvents();
                        await Task.Delay(subscriptionOptions.PollEvery);
                    }
                }
                catch (Exception e)
                {
                    this.onStopped?.Invoke(this, e);
                }

                this.onStopped?.Invoke(this, null);
            }, cancellationSource.Token);
        }

        public void Stop()
        {
            this.cancellationSource.Cancel();
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
                    loggingOptions.OnSuccess(ResponseInformation.FromSubscriptionReadResponse(feedResponse));
                    var events = new List<StorageEvent>();
                    string initialCheckpointValue;

                    foreach (var @event in feedResponse)
                    {
                        events.Add(DocumentDbStorageEvent.FromDocument(@event).ToStorageEvent(this.serializationTypeMap));
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