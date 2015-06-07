using System.Xml;
using System.Xml.Linq;

namespace Bend.Internal
{
    internal static class XmlReaderExtensions
    {
        public static XName CurrentName(this XmlReader self)
        {
            return (XNamespace)self.NamespaceURI + self.LocalName;
        }
    }
}
