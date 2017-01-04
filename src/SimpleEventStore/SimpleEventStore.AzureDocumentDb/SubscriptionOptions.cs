namespace SimpleEventStore.AzureDocumentDb
{
    public class SubscriptionOptions
    {
        public SubscriptionOptions(int maxItemCount)
        {
            this.MaxItemCount = maxItemCount;
        }

        public int MaxItemCount { get; }
    }
}