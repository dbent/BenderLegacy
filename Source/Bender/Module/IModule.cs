using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;

namespace Bender.Module
{
    public interface IModule // TODO: change this to IMessageHandler
    {
        void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence);
        void OnMessage(IMessage message);
    }
}
