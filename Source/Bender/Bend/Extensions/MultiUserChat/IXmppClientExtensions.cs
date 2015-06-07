using System.Runtime.CompilerServices;
using Bender.Bend.Clients;

namespace Bender.Bend.Extensions.MultiUserChat
{
    public static class XmppClientExtensions
    {
        private static readonly ConditionalWeakTable<IXmppClient, IClient> Extensions = new ConditionalWeakTable<IXmppClient, IClient>();

        public static IClient MultiUserChat(this IXmppClient self)
        {
            return Extensions.GetValue(self, CreateMultiUserChatClient);
        }

        private static IClient CreateMultiUserChatClient(IXmppClient xmppClient)
        {
            return new Client(xmppClient);
        }
    }
}
