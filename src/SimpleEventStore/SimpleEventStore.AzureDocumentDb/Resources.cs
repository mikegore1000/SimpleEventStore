
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SimpleEventStore.AzureDocumentDb
{
    internal static class Resources
    {
        public static string GetString(string resourceName)
        {
            using (var reader = new StreamReader(GetStream(resourceName)))
            {
                return reader.ReadToEnd();
            }
        }

        private static Stream GetStream(string resourceName)
        {
            resourceName = $"{typeof(Resources).FullName}.{resourceName}";
            return typeof(Resources).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName);
        }
    }
}