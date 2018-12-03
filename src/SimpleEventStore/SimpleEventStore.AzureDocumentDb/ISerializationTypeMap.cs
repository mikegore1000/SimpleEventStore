using System;

namespace SimpleEventStore.AzureDocumentDb
{
    public interface ISerializationTypeMap
    {
        Type GetTypeFromName(string typeName);

        string GetNameFromType(Type type);
    }
}