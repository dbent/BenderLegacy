using System.Xml;
using System.Xml.Linq;

namespace Bender.Internal.Extensions
{
    internal static class XmlReaderExtensions
    {
        public static XName CurrentName(this XmlReader self)
        {
            return (XNamespace)self.NamespaceURI + self.LocalName;
        }
    }
}
