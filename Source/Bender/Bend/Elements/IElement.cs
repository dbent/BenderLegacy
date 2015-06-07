using System.Xml.Linq;

namespace Bender.Bend.Elements
{
    public interface IElement
    {
        XElement Element { get; }
    }
}
