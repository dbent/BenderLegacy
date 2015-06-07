using System;
using System.Xml.Linq;

namespace Bender.Bend.Streams
{
    public interface IXmppClientStream : IObservable<XElement>, IDisposable
    {
        void Connect();
        void Disconnect();

        void Send(XElement stanza);
    }
}
