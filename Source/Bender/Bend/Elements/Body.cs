using System.Globalization;
using System.Xml.Linq;
using Bender.Bend.Constants;

namespace Bender.Bend.Elements
{
    public sealed class Body : IElement
    {
        private readonly XElement _element;

        public XElement Element => new XElement(_element);

        public Body(string value, Automatic<CultureInfo> lang)
        {
            _element = new XElement(ClientNamespace.Body);

            if (!lang.HasValue || lang.Value != null)
            {
                _element.Add(new XAttribute(XmlNamespace.Lang, lang.ValueOr(CultureInfo.CurrentCulture)));
            }

            if (value != null)
            {
                _element.Add(value);
            }
        }

        public static implicit operator XElement(Body body)
        {
            return body.Element;
        }
    }
}
