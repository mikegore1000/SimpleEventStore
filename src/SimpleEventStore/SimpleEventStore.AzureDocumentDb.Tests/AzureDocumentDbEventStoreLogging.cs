using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SimpleEventStore.Tests.Events;
using Xunit;
using Xunit.Abstractions;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    public class AzureDocumentDbEventStoreLogging
    {
        // TODO: Simplify tests when subscription supports cancellation tokens, should be able to cancel rather than running an if(Interlocked..) statement

        private readonly ITestOutputHelper output;

        public AzureDocumentDbEventStoreLogging(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task when_a_write_operation_is_successful_the_log_callback_is_called()
        {
            ResponseInformation response = null;
            var sut = new EventStore(await CreateStorageEngine(t => response = t));
            var streamId = Guid.NewGuid().ToString();

            await sut.AppendToStream(streamId, 0, new EventData(Guid.NewGuid(), new OrderCreated("TEST-ORDER")));

            Assert.NotNull(response);
            output.WriteLine($"Charge: {response.RequestCharge}");
            output.WriteLine($"Quota Usage: {response.CurrentResourceQuotaUsage}");
            output.WriteLine($"Max Resource Quote: {response.MaxResourceQuota}");
            output.WriteLine($"Response headers: {response.ResponseHeaders}");
        }

        [Fact]
        public async Task when_a_read_operation_is_successful_the_log_callback_is_called()
        {
            var logCount = 0;
            var sut = new EventStore(await CreateStorageEngine(t => logCount++));
            var streamId = Guid.NewGuid().ToString();

            await sut.AppendToStream(streamId, 0, new EventData(Guid.NewGuid(), new OrderCreated("TEST-ORDER")));
            await sut.ReadStreamForwards(streamId);

            Assert.Equal(2, logCount);
        }

        private static async Task<IStorageEngine> CreateStorageEngine(Action<ResponseInformation> onSuccessCallback, string databaseName = "LoggingTests")
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var documentDbUri = config["Uri"];
            var authKey = config["AuthKey"];
            var consistencyLevel = config["ConsistencyLevel"];
            ConsistencyLevel consistencyLevelEnum;

            if (!Enum.TryParse(consistencyLevel, true, out consistencyLevelEnum))
            {
                throw new Exception($"The ConsistencyLevel value {consistencyLevel} is not supported");
            }

            DocumentClient client = new DocumentClient(new Uri(documentDbUri), authKey);

            return await new AzureDocumentDbStorageEngineBuilder(client, databaseName)
                .UseCollection(o =>
                {
                    o.ConsistencyLevel = consistencyLevelEnum;
                    o.CollectionRequestUnits = 400;
                })
                .UseLogging(o =>
                {
                    o.Success = onSuccessCallback;
                })
                .Build()
                .Initialise();
        }
    }
}
