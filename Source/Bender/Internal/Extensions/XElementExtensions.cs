using System.Xml.Linq;

namespace Bender.Internal.Extensions
{
    internal static class XElementExtensions
    {
        public static bool Is(this XElement element, XName name)
        {
            return element.Name == name;
        }
    }
}
