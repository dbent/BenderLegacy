namespace Bender.Interfaces
{
    public interface IMessage
    {
        IAddress ReplyTo { get; }
        IAddress SenderAddress { get; }
        string SenderName { get; }

        bool IsAddressedAtMe { get; }
        bool IsFromMyself { get; }
        bool IsHistorical { get; }
        bool IsPrivate { get; }

        bool IsRelevant { get; } // TODO: encapsulate this better

        string Body { get; }
        string DirectedBody { get; } // TODO: needs a better name
        string FullBody { get; }
    }
}
