using System;

namespace Bender.Internal.Exceptions
{
    public class UnhandledEnumException : Exception
    {
        private const string FormatMessage = "Enumeration value {0} of type {1} was unhandled.";

        public UnhandledEnumException(Enum enumValue)
            : base(ExceptionMessage(enumValue)) { }

        private static string ExceptionMessage(Enum enumValue)
        {
            return string.Format(FormatMessage, enumValue, enumValue.GetType().Name);
        }
    }
}
