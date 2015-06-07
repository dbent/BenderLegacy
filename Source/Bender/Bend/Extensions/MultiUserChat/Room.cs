using System.Globalization;
using Bender.Bend.Clients;
using Bender.Bend.Elements;

namespace Bender.Bend.Extensions.MultiUserChat
{
    internal class Room : IRoom
    {
        private readonly Jid _roomJid;
        private readonly Jid _userJid;
        private readonly IXmppClient _xmppClient;       

        public Room(Jid room, Jid userJid, IXmppClient xmppClient)
        {
            _roomJid = room;
            _userJid = userJid;
            _xmppClient = xmppClient;
        }

        public void Leave()
        {
            _xmppClient.Send(_userJid, PresenceType.Unavailable, null, null);
        }

        public void SendMessage(string message)
        {
            _xmppClient.Send(_roomJid,
                type: MessageType.GroupChat,
                lang: new Automatic<CultureInfo>(),
                bodies: new[] { new Body(message, new Automatic<CultureInfo>()) }
            );
        }        
    }
}
