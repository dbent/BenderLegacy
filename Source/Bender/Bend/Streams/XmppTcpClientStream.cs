using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Bender.Bend.Constants;
using Bender.Bend.Utility;
using Bender.Internal.Exceptions;
using Bender.Internal.Extensions;
using Bender.Internal.IO;
using Bender.Internal.Text;

namespace Bender.Bend.Streams
{
    // TODO: Make STARTTLS a module
    // TODO: Make SASL a module
    // TODO: Handle stream errors
    // TODO: Add async versions of public methods
    // TODO: Consider all sorts of nasty threading issues
    public sealed class XmppTcpClientStream : IXmppClientStream
    {
        /// <summary>
        /// Represents possible internal states of a <see cref="XmppTcpClientStream"/>
        /// </summary>
        /// <remarks>
        /// <para>
        /// At any given moment a <see cref="XmppTcpClientStream"/> exists in one
        /// of a finite number of states as represented by the <see cref="State"/>
        /// enumeration.
        /// </para>
        /// <para>
        /// As various events occur the <see cref="XmppTcpClientStream"/>
        /// can transition between one state or another. However, transitions are not
        /// arbitrary and can only occur if they are defined in the 
        /// <see cref="XmppTcpClientStream.TransitionPaths"/> dictionary.
        /// </para>
        /// <para>
        /// <see cref="State"/> enumeration values do not have explicitly assigned values
        /// and should not be persisted outside an executing program.
        /// </para>
        /// <para>
        /// The initial state of a <see cref="XmppTcpClientStream"/> is <see cref="State.Disconnected"/>.
        /// </para>
        /// </remarks>
        private enum State : byte
        {
            Connected,
            Disconnected,
            DisconnectLocal,
            DisconnectRemote,
            Disposed,
            StreamStart,
            StreamNegotiation,            
        }

        /*
         *  digraph TransitionPaths {
         *      StreamStart -> StreamNegotiation;
         *      StreamStart -> DisconnectLocal;
         *      StreamStart -> DisconnectRemote;
         *      
         *      StreamNegotiation -> StreamStart;
         *      StreamNegotiation -> Connected;
         *      StreamNegotiation -> DisconnectLocal;
         *      StreamNegotiation -> DisconnectRemote;
         *      
         *      Connected -> DisconnectLocal;
         *      Connected -> DisconnectRemote;
         *      
         *      DisconnectLocal -> DisconnectRemote;
         *      DisconnectLocal -> Disconnected;
         *      
         *      DisconnectRemote -> Disconnected;
         *      
         *      Disconnected -> StreamStart;
         *      Disconnected -> Disposed;
         *  }
         */

        private static readonly Dictionary<State, HashSet<State>> TransitionPaths = new Dictionary<State, HashSet<State>>
            {
                {State.StreamStart, new HashSet<State> {
                    State.StreamNegotiation, State.DisconnectLocal, State.DisconnectRemote }},

                {State.StreamNegotiation, new HashSet<State> {
                    State.StreamStart, State.Connected, State.DisconnectLocal, State.DisconnectRemote }},

                {State.Connected, new HashSet<State> {
                    State.DisconnectLocal, State.DisconnectRemote }},
                
                {State.DisconnectLocal, new HashSet<State> {
                    State.DisconnectRemote, State.Disconnected}},

                {State.DisconnectRemote, new HashSet<State> {
                    State.Disconnected }},

                {State.Disconnected, new HashSet<State> {
                    State.StreamStart, State.Disposed }},

                {State.Disposed, new HashSet<State>()},
            };

        private static readonly TimeSpan DisconnectingTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan WhitespaceKeepAlivePeriod = TimeSpan.FromMinutes(5);

        private const string WhiteSpaceKeepAliveToken = " ";

        private readonly MultiObserver<XElement> _multiObserver = new MultiObserver<XElement>();

        private Timer _disconnectingTimer;
        private Timer _whiteSpaceKeepAliveTimer;
        private State _state = State.Disconnected;
        private readonly ManualResetEvent _connectedEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent _disconnectedEvent = new ManualResetEvent(false);
        private readonly Stack<Stream> _streams = new Stack<Stream>(2);        

        private readonly Jid _originalJid;
        private readonly string _password; // TODO: don't store the password

        private readonly EndPoint _endPoint;

        private XmlReader _xmlReader;
        private XmlWriter _xmlWriter;

        private IDisposable _observableStreamSubscription;

        private Jid _boundJid;
        
        private Stream CurrentStream => _streams.Peek();

        // TODO: Need the option to automatically determine the endpoint
        public XmppTcpClientStream(Jid jid, string password, EndPoint endPoint)
        {
            _originalJid = jid;
            _password = password;
            _endPoint = endPoint;

            _whiteSpaceKeepAliveTimer = new Timer(OnWhiteSpaceKeepAlive);
        }

        public void Connect()
        {
            AssertIn(State.Disconnected);

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(_endPoint);

            PushStream(new NetworkStream(socket, FileAccess.ReadWrite, true));

            TransitionToStreamStart();

            _connectedEvent.WaitOne();
        }

        public void Disconnect()
        {
            TransitionToLocalDisconnect();

            _disconnectedEvent.WaitOne();
        }

        public void Send(XElement element)
        {
            if (_state.Is(State.DisconnectLocal, State.DisconnectRemote))
            {   // We should be silent while we're in the process of disconnecting
                // TODO: Log that we're dropping stanzas
            }
            else
            {
                AssertIn(State.Connected);

                SendInternal(element);
            }
        }

        private T PushStream<T>(T stream) where T : Stream
        {
            DisposeObservableStreamSubscription();

            var observableStream = new ObservableStream(stream);

            _observableStreamSubscription = observableStream.Subscribe(new ConsoleStreamObserver(Encoding.Utf8NoBom, ConsoleColor.Cyan, ConsoleColor.Yellow));
            
            _streams.Push(observableStream);

            return stream;
        }

        private void SendInternal(XElement element)
        {
            element.WriteTo(_xmlWriter);
            _xmlWriter.Flush();

            ResetWhiteSpaceKeepAlive();
        }

        private async void StartReadLoopAsync()
        {
            // Keep a private reference to the reader when we start
            var reader = _xmlReader;

            while (await reader.ReadAsync())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Depth)
                        {
                            case 0:
                                if(reader.IsEmptyElement)
                                {
                                    TransitionToRemoteDisconnect();
                                }
                                else
                                {
                                    if (reader.CurrentName().Is(StreamsNamespace.Stream))
                                    {
                                        // TODO: validate version number
                                    }
                                    else
                                    {
                                        throw new Exception("Server sent a weird start element."); // TODO: More sepcific exception
                                    }
                                }

                                break;
                            case 1:
                                using (var st = reader.ReadSubtree())
                                {
                                    ProcessIncomingElement(XElement.Load(st));
                                }
                                break;
                            default:
                                throw new ImpossibleException("Reached start of element at depth greater than 1.");
                        }
                        break;
                    case XmlNodeType.EndElement:
                        switch (reader.Depth)
                        {
                            case 0:
                                if (reader.CurrentName().Is(StreamsNamespace.Stream))
                                {
                                    TransitionToRemoteDisconnect();
                                }
                                else
                                {
                                    throw new Exception("Reached end of root level element that was not the stream element."); // TODO: More sepcific exception
                                }                                
                                break;
                            default:
                                throw new ImpossibleException("Reached end of element other than the root.");
                        }
                        break;
                }
            }
        }

        private void ProcessIncomingElement(XElement stanza)
        {
            try
            {
                _multiObserver.OnNext(stanza);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            if (stanza.Is(StreamsNamespace.Features))
            {
                OnStreamFeatures(stanza);
            }
            else if (stanza.Is(TlsNamespace.Proceed))
            {
                // TODO: We have two ObservableStreams, the second of which is useless
                var sslStream = PushStream(new SslStream(CurrentStream));

                sslStream.AuthenticateAsClient(_originalJid.Domain);

                TransitionToStreamStart();
            }
            else if (stanza.Is(SaslNamespace.Success))
            {
                TransitionToStreamStart();
            }
            else if (stanza.Is(ClientNamespace.Iq))
            {
                if (stanza.Attribute("type").Value == "result" && stanza.Elements().Any(i => i.Is(BindNamespace.Bind)))
                {
                    var jidElement = stanza.Element(BindNamespace.Bind).Element(BindNamespace.Jid);

                    if (jidElement.IsNotNull())
                    {
                        _boundJid = Jid.Parse(jidElement.Value);
                    }
                    else
                    {
                        // TODO: error condition
                    }
                    
                    TransitionToConnected();
                }
            }
        }

        private void OnStreamFeatures(XElement streamFeatures)
        {
            foreach (var feature in streamFeatures.Elements())
	        {
                if (feature.Is(TlsNamespace.StartTls))
                {
                    SendInternal(new XElement(TlsNamespace.StartTls));

                    break;
                }
                else if (feature.Is(SaslNamespace.Mechanisms))
                {
                    var user = _originalJid.Local.ToUtf8Bytes();
                    var pass = _password.ToUtf8Bytes();
                    var nul = "\0".ToUtf8Bytes();

                    var message = Enumerable.Empty<byte>()
                        .Concat(user)
                        .Concat(nul)
                        .Concat(user)
                        .Concat(nul)
                        .Concat(pass).ToBase64();

                    SendInternal(new XElement(SaslNamespace.Auth,
                        new XAttribute("mechanism", "PLAIN"),
                        message));
                }
                else if (feature.Is(BindNamespace.Bind))
                {
                    SendInternal(new XElement(ClientNamespace.Iq,
                        new XAttribute("id", Guid.NewGuid().ToString()),
                        new XAttribute("type", "set"),
                        new XElement(BindNamespace.Bind)));
                }
	        }
        }

        private void ResetWhiteSpaceKeepAlive()
        {
            _whiteSpaceKeepAliveTimer.Change(WhitespaceKeepAlivePeriod, WhitespaceKeepAlivePeriod);
        }

        private void OnWhiteSpaceKeepAlive(object state)
        {
            _xmlWriter.WriteWhitespace(WhiteSpaceKeepAliveToken);
            _xmlWriter.Flush();
        }

        #region State Machine

        private void TransitionToStreamStart()
        {
            var prevState = AssertAndDoTransitionTo(State.StreamStart);

            if (prevState.Is(State.StreamNegotiation))
            {
                DisposeXmlWriter();
                DisposeXmlReader();
            }

            _xmlReader = XmlReader.Create(CurrentStream, new XmlReaderSettings
                {
                    Async = true,
                    CloseInput = false,
                    ConformanceLevel = ConformanceLevel.Fragment,
                });

            _xmlWriter = XmlWriter.Create(CurrentStream, new XmlWriterSettings
                {
                    Async = true,
                    CloseOutput = false,
                    ConformanceLevel = ConformanceLevel.Fragment,
                    Encoding = Encoding.Utf8NoBom,
                    WriteEndDocumentOnClose = false,
                });

            StartReadLoopAsync();

            _xmlWriter.WriteRaw(new XDeclaration("1.0", "utf-8", null).ToString());
            _xmlWriter.WriteStartElement("stream", "stream", Namespaces.Streams.ToString());
            _xmlWriter.WriteAttributeString("from", _originalJid.Bare.ToString()); // TODO: don't send the from header if the stream is not secure
            _xmlWriter.WriteAttributeString("to", _originalJid.Domain);
            _xmlWriter.WriteAttributeString("version", "1.0");
            _xmlWriter.WriteAttributeString("xml", "lang", null, "en");
            _xmlWriter.WriteAttributeString("xmlns", null, Namespaces.Client.ToString());
            _xmlWriter.WriteAttributeString("xmlns", "stream", null, Namespaces.Streams.ToString());
            _xmlWriter.WriteString(string.Empty); // to force closure of opening element

            _xmlWriter.Flush();

            TransitionToStreamNegotiation();
        }

        private void TransitionToStreamNegotiation()
        {
            AssertAndDoTransitionTo(State.StreamNegotiation);            
        }

        private void TransitionToConnected()
        {
            AssertAndDoTransitionTo(State.Connected);

            _connectedEvent.Set();

            ResetWhiteSpaceKeepAlive();
        }

        private void TransitionToLocalDisconnect()
        {
            AssertAndDoTransitionTo(State.DisconnectLocal);

            _xmlWriter.WriteEndElement();
            _xmlWriter.Flush();

            _disconnectingTimer = new Timer(s =>
                {
                    if (_state.Is(State.DisconnectLocal))
                    {
                        TransitionToDisconnected();
                    }
                }, null, DisconnectingTimeout, TimeSpan.Zero);
        }

        private void TransitionToRemoteDisconnect()
        {
            var prevState = AssertAndDoTransitionTo(State.DisconnectRemote);

            if (!prevState.Is(State.DisconnectLocal))
            {
                _xmlWriter.WriteEndElement();
                _xmlWriter.Flush();
            }

            TransitionToDisconnected();
        }

        private void TransitionToDisconnected()
        {
            /* TODO: Compliance: Not sure if SslStream will send TLS close_notify
             * 
             * http://xmpp.org/rfcs/rfc6120.html#streams-close
             */

            // TODO: need to reinitialize all the disposables if we reconnnect later

            DisposeDisconnectingTimer();
            DisposeWhiteSpaceKeepAliveTimer();

            AssertAndDoTransitionTo(State.Disconnected);

            DisposeXmlWriter();
            DisposeXmlReader();
            DisposeStreams();
            DisposeObservableStreamSubscription();

            // TODO: reset the various wait handles
            _disconnectedEvent.Set();
        }

        private void TransitionToDisposed()
        {
            AssertAndDoTransitionTo(State.Disposed);

            DisposeXmlWriter();
            DisposeXmlReader();
            DisposeStreams();
            DisposeObservableStreamSubscription();

            try
            {
                _multiObserver.OnCompleted();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
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
            if (_state.Is(State.StreamStart, State.StreamNegotiation, State.Connected))
            {
                Disconnect();
            }

            TransitionToDisposed();
        }

        private void DisposeDisconnectingTimer()
        {
            if (_disconnectingTimer.IsNotNull())
            {
                _disconnectingTimer.Dispose();
                _disconnectingTimer = null;
            }
        }

        private void DisposeWhiteSpaceKeepAliveTimer()
        {
            if (_whiteSpaceKeepAliveTimer.IsNotNull())
            {
                _whiteSpaceKeepAliveTimer.Dispose();
                _whiteSpaceKeepAliveTimer = null;
            }
        }

        private void DisposeObservableStreamSubscription()
        {
            if (_observableStreamSubscription.IsNotNull())
            {
                _observableStreamSubscription.Dispose();
                _observableStreamSubscription = null;
            }
        }

        private void DisposeStreams()
        {
            while (_streams.Count != 0)
            {
                var stream = _streams.Pop();
                if (stream.IsNotNull())
                {
                    stream.Dispose();
                }
            }
        }

        private void DisposeXmlReader()
        {
            if (_xmlReader.IsNotNull())
            {
                try
                {
                    _xmlReader.Dispose();
                }
                catch (InvalidOperationException e)
                {   /* TODO: Find a way to dispose of XmlReader more gracefully
                     *
                     * http://stackoverflow.com/questions/12015279/disposing-of-xmlreader-with-pending-async-read
                     */

                    if (!e.Message.Contains("An asynchronous operation is already in progress."))
                    {
                        throw;
                    }
                }

                _xmlReader = null;
            }
        }

        private void DisposeXmlWriter()
        {
            if (_xmlWriter.IsNotNull())
            {
                _xmlWriter.Dispose();
                _xmlWriter = null;
            }
        }

        #endregion

        #region Asserts

        private State AssertAndDoTransitionTo(State state)
        {
            if (!TransitionPaths[_state].Contains(state))
            {
                if (_state == State.Disposed)
                {
                    throw new ObjectDisposedException(null);
                }
                else
                {
                    throw new InvalidOperationException("Attempted to transition from {0} to {1}.".FormatWith(_state, state));
                }
            }

            var prevState = _state;
            _state = state;

            return prevState;
        }

        private void AssertIn(params State[] states)
        {
            if (!_state.Is(states))
            {
                switch (_state)
                {
                    case State.Disposed:
                        throw new ObjectDisposedException(null);
                    default:
                        throw new InvalidOperationException("Stream is in state {0}, expected it to be in one of the following states: {1}."
                            .FormatWith(_state, states.Select(i => i.ToString()).Aggregate((i, j) => i + ", " + j)));
                }
            }
        }

        #endregion        
    }
}
