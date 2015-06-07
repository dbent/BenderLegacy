using System.Collections.Generic;
using System.Linq;

namespace Bender.Internal.Extensions
{
    internal static class ObjectExtensions
    {
        public static IEnumerable<T> AsEnumerable<T>(this T obj)
        {
            return Enumerable.Repeat(obj, 1);
        }

        public static bool Is<T, U>(this T self, U value) where T : U
        {
            return Equals(self, value);
        }

        public static bool Is<T, U>(this T self, params U[] values)  where T : U
        {
            return values.Contains(self);
        }

        public static bool IsNotNull(this object obj)
        {
            return obj != null;
        }
    }
}
