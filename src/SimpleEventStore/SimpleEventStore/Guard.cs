using System;

namespace SimpleEventStore
{
    public static class Guard
    {
        public static void IsNotNullOrEmpty(string paramName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("The value cannot be a null, empty string or contain only whitespace", paramName);
            }
        }

        public static void IsNotNull(string paramName, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName, "The value cannot be null");
            }
        }
    }
}