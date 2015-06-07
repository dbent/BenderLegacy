using Bender.Interfaces;

namespace Bender.Common
{
    public class MessageData
    {
        public IAddress ReplyTo { get; }
        public IAddress SenderAddress { get; }
        public string SenderName { get; }

        public bool IsFromMyself { get; }
        public bool IsHistorical { get; }
        public bool IsPrivate { get; }

        public string Body { get; }

        public MessageData(IAddress replyTo, IAddress senderAddress, string senderName, string body, bool isFromMyself, bool isHistorical, bool isPrivate)
        {
            ReplyTo = replyTo;
            SenderAddress = senderAddress;
            SenderName = senderName;

            IsFromMyself = isFromMyself;
            IsHistorical = isHistorical;
            IsPrivate = isPrivate;

            Body = body;
        }
    }
}
