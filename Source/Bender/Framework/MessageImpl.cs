using Bender.Common;
using Bender.Interfaces;

namespace Bender.Framework
{
    internal class MessageImpl : IMessage
    {
        public IAddress ReplyTo { get; }
        public IAddress SenderAddress { get; }
        public string SenderName { get; }

        public bool IsAddressedAtMe { get; }
        public bool IsFromMyself { get; }
        public bool IsHistorical { get; }
        public bool IsPrivate { get; }        

        public string Body { get; }
        public string DirectedBody { get; }
        public string FullBody  { get; }

        public bool IsRelevant => !IsFromMyself && !IsHistorical && (IsAddressedAtMe || IsPrivate);

        public MessageImpl(MessageData message, string directedBody, bool isAddressedAtMe)
        {
            ReplyTo = message.ReplyTo;
            SenderAddress = message.SenderAddress;
            SenderName = message.SenderName;

            IsAddressedAtMe = isAddressedAtMe;
            IsFromMyself = message.IsFromMyself;
            IsHistorical = message.IsHistorical;
            IsPrivate = message.IsPrivate;

            Body = isAddressedAtMe ? directedBody : message.Body;
            DirectedBody = directedBody;
            FullBody = message.Body;            
        }        
    }
}
