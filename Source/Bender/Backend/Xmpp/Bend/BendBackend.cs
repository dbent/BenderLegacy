using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bender.Bend;
using Bender.Bend.Clients;
using Bender.Bend.Constants;
using Bender.Bend.Elements;
using Bender.Bend.Extensions.MultiUserChat;
using Bender.Bend.Streams;
using Bender.Bend.Utility;
using Bender.Common;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Internal.Extensions;

namespace Bender.Backend.Xmpp.Bend
{
    internal class BendBackend : IBackend, IObserver<XElement>
    {
        private readonly MultiObserver<MessageData> _multiObserver = new MultiObserver<MessageData>();

        private readonly IConfiguration _configuration;
        private readonly IXmppClient _client;
        private readonly IDisposable _observableSubscription;
        private readonly Jid _jid;

        public BendBackend(IConfiguration configuration)
        {
            _configuration = configuration;
            
            _jid = Jid.Parse(_configuration.Jid);
            _client = new XmppClient(new XmppTcpClientStream(_jid, _configuration.Password, new DnsEndPoint(_jid.Domain, 5222)));
            _observableSubscription = _client.Subscribe(this);
        }

        public async Task ConnectAsync()
        {
            await Task.Run(() =>
                {
                    _client.Connect();
                    foreach (var room in _configuration.Rooms)
                    {
                        _client.MultiUserChat().JoinRoom(Jid.Parse(room), _configuration.Name);
                    }
                });
        }

        public async Task DisconnectAsync()
        {
            await Task.Run(() => _client.Disconnect());
        }

        public async Task SendMessageAsync(IAddress address, string body)
        {
            await Task.Run(() =>
                {
                    var xAddress = address as Address;
                    if (xAddress != null)
                    {
                        _client.Send(xAddress.Jid, xAddress.MessageType, new Automatic<CultureInfo>(), new Body(body, new Automatic<CultureInfo>(null)).AsEnumerable());
                    }
                });
        }

        public void Dispose()
        {
            _client.Dispose();
            _observableSubscription.Dispose();
            //this.multiObserver.OnCompleted(); // TODO: need to make sure we don't send OnCompleted twice
        }

        public IDisposable Subscribe(IObserver<MessageData> observer)
        {
            return _multiObserver.Add(observer);
        }

        public void OnCompleted()
        {
            _multiObserver.OnCompleted();
        }

        public void OnError(Exception error)
        {
            _multiObserver.OnError(error);
        }

        public void OnNext(XElement value)
        {
            if (value.Is(ClientNamespace.Message))
            {
                var type = value.Attribute("type");
                if (type.IsNotNull())
                {
                    var isTypeChat = type.Value.Equals(MessageType.Chat.ToString(), StringComparison.OrdinalIgnoreCase);
                    var isTypeGroupChat = type.Value.Equals(MessageType.GroupChat.ToString(), StringComparison.OrdinalIgnoreCase);

                    if (isTypeChat || isTypeGroupChat)
                    {
                        var body = value.Element(ClientNamespace.Body);
                        if (body != null)
                        {
                            var from = value.Attribute("from");

                            if (from != null)
                            {
                                var fromJid = Jid.Parse(from.Value);

                                _multiObserver.OnNext(new MessageData(
                                    replyTo: new Address(isTypeChat ? fromJid : fromJid.Bare, isTypeChat ? MessageType.Chat : MessageType.GroupChat),
                                    senderAddress: new Address(fromJid, MessageType.Chat),
                                    senderName: isTypeChat ? fromJid.Local : fromJid.Resource,
                                    body: body.Value,
                                    isFromMyself: isTypeChat ? Equals(fromJid.Bare, _jid.Bare) : string.Equals(fromJid.Resource, _configuration.Name, StringComparison.OrdinalIgnoreCase),
                                    isHistorical: value.Element(DelayNamespace.Delay) != null,
                                    isPrivate: isTypeChat
                                ));
                            }                            
                        }
                    }
                }
            }
        }

        private class Address : IAddress
        {
            public readonly Jid Jid;
            public readonly MessageType MessageType;

            public Address(Jid jid, MessageType messageType)
            {
                Jid = jid;
                MessageType = messageType;
            }
        }
    }
}
