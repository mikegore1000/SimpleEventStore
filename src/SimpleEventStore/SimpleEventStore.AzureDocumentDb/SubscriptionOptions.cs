using System;

namespace SimpleEventStore.AzureDocumentDb
{
    public class SubscriptionOptions
    {
        public SubscriptionOptions()
        {
            this.MaxItemCount = 100;
            this.PollEvery = TimeSpan.FromSeconds(5);
        }

        public int MaxItemCount { get; set; }

        public TimeSpan PollEvery { get; set; }
    }
}