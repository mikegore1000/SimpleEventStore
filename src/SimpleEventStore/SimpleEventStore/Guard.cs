using System;

namespace SimpleEventStore
{
    internal static class Guard
    {
        internal static void IsNotNullOrEmpty(string paramName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("The value cannot be a null, empty string or contain only whitespace", paramName);
            }
        }

        internal static void IsNotNull(string paramName, object value)
        {
            if (value == null)
            {
                throw new ArgumentException("The value cannot be a null, empty string or contain only whitespace", paramName);
            }
        }
    }
}