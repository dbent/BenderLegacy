using System.Xml.Linq;

namespace Bender.Bend.Elements
{
    public abstract class StanzaType : IAttribute
    {
        private readonly XAttribute _attribute;

        public XAttribute Attribute => new XAttribute(_attribute);

        protected StanzaType(string value)
        {
            _attribute = new XAttribute("type", value);
        }

        public override string ToString()
        {
            return _attribute.Value;
        }

        public static implicit operator XAttribute(StanzaType type)
        {
            return type.Attribute;
        }        
    }
}
