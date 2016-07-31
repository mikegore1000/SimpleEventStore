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

        public static void IsNotNullOrEmptyOrAllStream(string paramName, string value)
        {
            IsNotNullOrEmpty(paramName, value);

            if (value == "$all")
            {
                throw new ArgumentException("The value cannot be the $all stream", paramName);
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