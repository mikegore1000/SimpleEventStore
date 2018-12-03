using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SimpleEventStore.AzureDocumentDb
{
    public class ConfigurableSerializationTypeMap : ISerializationTypeMap
    {
        private readonly Dictionary<string, Type> typeMap = new Dictionary<string, Type>();
        private readonly Dictionary<Type, string> nameMap = new Dictionary<Type, string>();

        public ConfigurableSerializationTypeMap RegisterType(string eventType, Type type)
        {
            Guard.IsNotNullOrEmpty(nameof(eventType), eventType);
            Guard.IsNotNull(nameof(type), type);

            typeMap.Add(eventType, type);
            nameMap.Add(type, eventType);
            return this;
        }

        public ConfigurableSerializationTypeMap RegisterTypes(Assembly assembly, Func<Type, bool> matchFunc, Func<Type, string> namingFunc)
        {
            Guard.IsNotNull(nameof(assembly), assembly);
            Guard.IsNotNull(nameof(matchFunc), matchFunc);
            Guard.IsNotNull(nameof(namingFunc), namingFunc);
            bool matchesFound = false;

            foreach (var type in assembly.GetTypes().Where(matchFunc))
            {
                matchesFound = true;
                RegisterType(namingFunc(type), type);
            }

            if (!matchesFound)
            {
                throw new NoTypesFoundException("The matchFunc matched no types in the assembly");
            }

            return this;
        }

        public Type GetTypeFromName(string typeName)
        {
            return typeMap[typeName];
        }

        public string GetNameFromType(Type type)
        {
            return nameMap[type];
        }
    }

    public class NoTypesFoundException : Exception
    {
        public NoTypesFoundException(string message) : base(message)
        { }
    }
}
