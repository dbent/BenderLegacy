using System;
using System.Threading.Tasks;
using Bender.Common;

namespace Bender.Interfaces
{
    public interface IBackend : IDisposable, IObservable<MessageData>
    {
        Task ConnectAsync();
        Task DisconnectAsync();

        Task SendMessageAsync(IAddress address, string body);
    }
}
