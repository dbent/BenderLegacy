using System.Xml.Linq;

namespace Bend.Internal
{
    internal static class XElementExtensions
    {
        public static bool Is(this XElement element, XName name)
        {
            return element.Name == name;
        }
    }
}
