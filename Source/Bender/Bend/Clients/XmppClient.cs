using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Bender.Bend.Constants;
using Bender.Bend.Elements;
using Bender.Bend.Streams;
using Bender.Bend.Utility;

namespace Bender.Bend.Clients
{
    public sealed class XmppClient : IXmppClient, IObserver<XElement>
    {
        private const string ExceptionConnected = "Session is already connected.";
        private const string ExceptionNotConnected = "Session is not connected.";

        private readonly MultiObserver<XElement> _multiObserver = new MultiObserver<XElement>();

        private bool _connected;
        private bool _disposed;

        private IXmppClientStream _clientStream;

        public XmppClient(IXmppClientStream clientStream)
        {
            _clientStream = clientStream;
            _clientStream.Subscribe(this);
        }

        #region Public API

        public void Connect()
        {
            AssertNotDisposed();
            AssertNotConnected();

            _clientStream.Connect();

            SendPresenceInternal(null, null, null, null, false);

            _connected = true;
        }

        public void Disconnect()
        {
            AssertNotDisposed();
            AssertConnected();

            _clientStream.Disconnect();

            _connected = false;
        }

        public void Send(XElement stanza)
        {
            SendInternal(stanza);
        }        

        public void Send(Jid to, MessageType type,
            Automatic<CultureInfo> lang, IEnumerable<Body> bodies)
        {
            SendMessageInternal(to, type, lang, bodies, true);
        }

        public void Send(Jid to, PresenceType type,
            Automatic<CultureInfo> lang, IEnumerable<XElement> extendedContent)
        {
            SendPresenceInternal(to, type, lang, extendedContent, true);
        }

        #endregion

        #region Private API

        private void SendInternal(XElement stanza, bool check = true)
        {
            if (check)
            {
                AssertNotDisposed();
                AssertConnected();
            }

            _clientStream.Send(stanza);
        }

        private void SendMessageInternal(Jid to, MessageType type,
            Automatic<CultureInfo> lang, IEnumerable<Body> bodies,
            bool check)
        {
            var stanza = new XElement(ClientNamespace.Message, new XAttribute("id", GenerateId()));

            stanza.Add(new XAttribute("to", to));

            if (type != null)
            {
                stanza.Add((XAttribute)type);
            }

            if (!lang.HasValue || lang.Value != null)
            {
                stanza.Add(new XAttribute(XmlNamespace.Lang, lang.ValueOr(CultureInfo.CurrentCulture)));
            }

            if (bodies != null && bodies.Any())
            {
                stanza.Add(bodies.Select(i => (XElement)i));
            }

            SendInternal(stanza, check);
        }

        private void SendPresenceInternal(Jid to, PresenceType type,
            Automatic<CultureInfo> lang, IEnumerable<XElement> extendedContent,
            bool check)
        {
            var stanza = new XElement(ClientNamespace.Presence, new XAttribute("id", GenerateId()));

            if (to != null)
            {
                stanza.Add(new XAttribute("to", to));
            }

            if (type != null)
            {
                stanza.Add((XAttribute)type);
            }

            if (!lang.HasValue || lang.Value != null)
            {
                stanza.Add(new XAttribute(XmlNamespace.Lang, lang.ValueOr(CultureInfo.CurrentCulture)));
            }

            if (extendedContent != null && extendedContent.Any())
            {
                stanza.Add(extendedContent);
            }

            SendInternal(stanza, check);
        }

        private static string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }

        #endregion

        #region Observable

        public IDisposable Subscribe(IObserver<XElement> observer)
        {
            return _multiObserver.Add(observer);
        }

        #endregion

        #region Disposable

        public void Dispose()
        {
            DisposeClientStream();

            _disposed = true;
        }

        private void DisposeClientStream()
        {
            if (_clientStream != null)
            {
                _clientStream.Dispose();
                _clientStream = null;
            }
        }

        #endregion

        #region Asserts

        private void AssertNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null);
            }
        }

        private void AssertConnected()
        {
            if (!_connected)
            {
                throw new InvalidOperationException(ExceptionNotConnected);
            }
        }

        private void AssertNotConnected()
        {
            if (_connected)
            {
                throw new InvalidOperationException(ExceptionConnected);
            }
        }

        #endregion                
    
        public void OnCompleted()
        {
            try
            {
                _multiObserver.OnCompleted();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        public void OnError(Exception error)
        {
            try
            {
                _multiObserver.OnError(error);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        public void OnNext(XElement value)
        {
            try
            {
                _multiObserver.OnNext(value);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
}
