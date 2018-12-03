using System;

namespace SimpleEventStore.AzureDocumentDb
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
