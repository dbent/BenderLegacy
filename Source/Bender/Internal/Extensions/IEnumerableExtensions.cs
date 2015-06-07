using System;
using System.Collections.Generic;
using System.Linq;

namespace Bender.Internal.Extensions
{
    internal static class EnumerableExtensions
    {
        public static string ToBase64(this IEnumerable<byte> self)
        {
            return Convert.ToBase64String(self.ToArray());
        }
    }
}
