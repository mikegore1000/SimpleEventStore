using System;

namespace SimpleEventStore.AzureDocumentDb
{
    public class SubscriptionsNotConfiguredException : Exception
    {
        public SubscriptionsNotConfiguredException(string message) : base(message)
        {
        }
    }
}
