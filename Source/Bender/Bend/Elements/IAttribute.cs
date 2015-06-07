using System.Xml.Linq;

namespace Bender.Bend.Elements
{
    public interface IAttribute
    {
        XAttribute Attribute { get; }
    }
}
