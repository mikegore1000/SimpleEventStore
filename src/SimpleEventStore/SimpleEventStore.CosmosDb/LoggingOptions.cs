using System;

namespace SimpleEventStore.CosmosDb
{
    public class LoggingOptions
    {
        public Action<ResponseInformation> Success { get; set; }

        internal void OnSuccess(ResponseInformation response)
        {
            Success?.Invoke(response);
        }
    }
}
