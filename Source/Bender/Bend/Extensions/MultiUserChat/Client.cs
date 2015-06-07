using System.Xml.Linq;
using Bender.Bend.Clients;
using Bender.Bend.Constants;

namespace Bender.Bend.Extensions.MultiUserChat
{
    internal class Client : IClient
    {
        private readonly IXmppClient _xmppClient;

        public Client(IXmppClient xmppClient)
        {
            _xmppClient = xmppClient;
        }

        public IRoom JoinRoom(Jid room, string nickname)
        {
            // TODO: only return after we've actually joined the room
            var userJid = new Jid(room.Local, room.Domain, nickname);

            _xmppClient.Send(userJid, null, null, new[] { new XElement(MucNamespace.X) });

            return new Room(room, userJid, _xmppClient);
        }
    }
}
