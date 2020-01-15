using System;
using System.Reflection;
using NUnit.Framework;
using SimpleEventStore.Tests.Events;

namespace SimpleEventStore.CosmosDb.Tests
{
    [TestFixture]
    public class ConfigurableTypeMapSerializationBinderTests
    {
        [Test]
        public void when_registering_a_type_with_a_null_event_type_then_an_exception_is_thrown()
        {
            var sut = new ConfigurableSerializationTypeMap();
            Assert.Throws<ArgumentException>(() => sut.RegisterType(null, typeof(OrderCreated)));
        }

        [Test]
        public void when_registering_a_type_with_a_null_type_then_an_exception_is_thrown()
        {
            var sut = new ConfigurableSerializationTypeMap();
            Assert.Throws<ArgumentNullException>(() => sut.RegisterType("TEST", null));
        }

        [Test]
        public void when_registering_types_with_a_null_assembly_then_an_exception_is_thrown()
        {
            var sut = new ConfigurableSerializationTypeMap();
            Assert.Throws<ArgumentNullException>(() => sut.RegisterTypes(null, t => true, t => t.Name));
        }

        [Test]
        public void when_registering_events_with_a_null_match_function_then_an_exception_is_thrown()
        {
            var sut = new ConfigurableSerializationTypeMap();
            Assert.Throws<ArgumentNullException>(() => sut.RegisterTypes(typeof(OrderCreated).GetTypeInfo().Assembly, null, t => t.Name));
        }

        [Test]
        public void when_registering_types_with_a_null_naming_function_then_an_exception_is_thrown()
        {
            var sut = new ConfigurableSerializationTypeMap();
            Assert.Throws<ArgumentNullException>(() => sut.RegisterTypes(typeof(OrderCreated).GetTypeInfo().Assembly, t => true, null));
        }

        [Test]
        public void when_registering_a_type_then_the_type_can_be_found()
        {
            var sut = new ConfigurableSerializationTypeMap();
            sut.RegisterType("OrderCreated", typeof(OrderCreated));

            Assert.That(sut.GetTypeFromName("OrderCreated"), Is.EqualTo(typeof(OrderCreated)));
        }

        [Test]
        public void when_registering_multiple_types_then_the_type_can_be_found()
        {
            var sut = new ConfigurableSerializationTypeMap();
            sut.RegisterTypes(typeof(OrderCreated).GetTypeInfo().Assembly, t => t.Namespace != null && t.Namespace.EndsWith("Events"), t => t.Name);

            Assert.That(sut.GetTypeFromName("OrderCreated"), Is.EqualTo(typeof(OrderCreated)));
        }

        [Test]
        public void when_registering_a_type_then_the_name_can_be_found()
        {
            var sut = new ConfigurableSerializationTypeMap();
            sut.RegisterType("OrderCreated", typeof(OrderCreated));

            Assert.That(sut.GetNameFromType(typeof(OrderCreated)), Is.EqualTo("OrderCreated"));
        }

        [Test]
        public void when_registering_multiple_types_if_no_types_are_found_then_an_exception_is_thrown()
        {
            var sut = new ConfigurableSerializationTypeMap();
            Assert.Throws<NoTypesFoundException>(() => sut.RegisterTypes(typeof(OrderCreated).GetTypeInfo().Assembly, t => false, t => t.Name));
        }
    }
}
