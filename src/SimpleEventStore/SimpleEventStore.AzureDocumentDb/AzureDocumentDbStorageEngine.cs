using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace SimpleEventStore.AzureDocumentDb
{
    public class AzureDocumentDbStorageEngine : IStorageEngine
    {
        private readonly IDocumentClient client;
        private readonly string databaseName;

        public AzureDocumentDbStorageEngine(IDocumentClient client, string databaseName)
        {
            this.client = client;
            this.databaseName = databaseName;
        }

        public async Task Initialise()
        {
            await CreateDatabaseIfItDoesNotExist();
            await CreateCollectionIfItDoesNotExist();
        }

        public Task AppendToStream(string streamId, IEnumerable<StorageEvent> events)
        {
            // TODO: Need to invoke a stored procedure in order to insert all the events in an atomic manner
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<StorageEvent>> ReadStreamForwards(string streamId, int startPosition, int numberOfEventsToRead)
        {
            throw new System.NotImplementedException();
        }

        private async Task CreateDatabaseIfItDoesNotExist()
        {
            try
            {
                await client.CreateDatabaseAsync(new Database { Id = databaseName });
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode != HttpStatusCode.Conflict)
                {
                    throw;
                }
            }
        }

        private async Task CreateCollectionIfItDoesNotExist()
        {
            // TODO: Need to optimise the indexing policy

            var collection = new DocumentCollection
            {
                Id = "Commits"
            };

            // TODO: Make this configurable by the consuming app - need to see if this can be updated, if so then we should attempt to update
            var requestOptions = new RequestOptions
            {
            };

            try
            {
                await client.CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(databaseName), collection, requestOptions);
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode != HttpStatusCode.Conflict)
                {
                    throw;
                }
            }
        }
    }
}
