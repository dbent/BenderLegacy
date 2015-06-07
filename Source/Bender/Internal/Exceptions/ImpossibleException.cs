using System;
using Bender.Internal.Extensions;

namespace Bender.Internal.Exceptions
{
    public class ImpossibleException : Exception
    {
        public ImpossibleException(string message)
            : base(ExceptionMessage(message)) { }

        private static string ExceptionMessage(string message)
        {
            return "A situation occured that should have been impossible. {0}".FormatWith(message);
        }
    }
}
