using System;
using Microsoft.Azure.Documents.Client;
using Xunit;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    public class AzureDocumentDbStorageEngineBuilderTests
    {
        [Fact]
        public void when_creating_an_instance_the_document_client_must_be_supplied()
        {
            Assert.Throws<ArgumentNullException>(() => new AzureDocumentDbStorageEngineBuilder(null, "Test"));
        }

        [Fact]
        public void when_creating_an_instance_the_database_name_must_be_supplied()
        {
            Assert.Throws<ArgumentException>(() => new AzureDocumentDbStorageEngineBuilder(CreateClient(), null));
        }

        [Fact]
        public void when_setting_collection_settings_a_callback_must_be_supplied()
        {
            var builder = new AzureDocumentDbStorageEngineBuilder(CreateClient(), "Test");
            Assert.Throws<ArgumentNullException>(() => builder.UseCollection(null));
        }

        [Fact]
        public void when_setting_subscription_settings_a_callback_must_be_supplied()
        {
            var builder = new AzureDocumentDbStorageEngineBuilder(CreateClient(), "Test");
            Assert.Throws<ArgumentNullException>(() => builder.UseCollection(null));
        }

        [Fact]
        public void when_setting_logging_settings_a_callback_must_be_supplied()
        {
            var builder = new AzureDocumentDbStorageEngineBuilder(CreateClient(), "Test");
            Assert.Throws<ArgumentNullException>(() => builder.UseLogging(null));
        }

        [Fact]
        public void when_setting_the_type_map_it_must_be_supplied()
        {
            var builder = new AzureDocumentDbStorageEngineBuilder(CreateClient(), "Test");
            Assert.Throws<ArgumentNullException>(() => builder.UseTypeMap(null));
        }

        private static DocumentClient CreateClient()
        {
            var client = new DocumentClient(new Uri("https://localhost:8081/"), "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
            return client;
        }
    }
}
