using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using Bender.Bend.Elements;

namespace Bender.Bend.Clients
{
    public interface IXmppClient : IObservable<XElement>, IDisposable
    {
        void Connect();
        void Disconnect();

        void Send(XElement stanza);

        void Send(Jid to, MessageType type,
            Automatic<CultureInfo> lang, IEnumerable<Body> bodies);

        void Send(Jid to, PresenceType type,
            Automatic<CultureInfo> lang, IEnumerable<XElement> extendedContent);
    }
}
