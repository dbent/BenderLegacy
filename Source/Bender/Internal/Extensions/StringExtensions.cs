using Bender.Internal.Text;

namespace Bender.Internal.Extensions
{
    internal static class StringExtensions
    {
        public static string FormatWith(this string self, object arg0)
        {
            return string.Format(self, arg0);
        }

        public static string FormatWith(this string self, params object[] args)
        {
            return string.Format(self, args);
        }

        public static byte[] ToUtf8Bytes(this string self)
        {
            return Encoding.Utf8NoBom.GetBytes(self);
        }
    }
}
