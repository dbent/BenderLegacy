namespace Bender.Bend.Extensions.MultiUserChat
{
    public interface IRoom
    {
        void Leave();
        void SendMessage(string message);        
    }
}
