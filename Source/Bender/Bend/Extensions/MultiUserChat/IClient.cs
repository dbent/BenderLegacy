namespace Bender.Bend.Extensions.MultiUserChat
{
    public interface IClient
    {
        IRoom JoinRoom(Jid room, string nickname);
    }
}
