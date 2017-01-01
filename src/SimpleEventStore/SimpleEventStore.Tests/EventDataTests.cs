using System;
using Xunit;

namespace SimpleEventStore.Tests
{
    public class EventDataTests
    {
        [Fact]
        public void when_creating_an_instance_the_event_body_must_be_supplied()
        {
            Assert.Throws<ArgumentNullException>(() => new EventData(Guid.NewGuid(), null));
        }
    }
}
