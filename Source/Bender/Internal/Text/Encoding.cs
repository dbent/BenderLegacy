using System.Text;

namespace Bender.Internal.Text
{
    public static class Encoding
    {
        public static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
    }
}
