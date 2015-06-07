using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bender.Configuration;
using Bender.Persistence;

namespace Bender.Module
{
    public interface IModule // TODO: change this to IMessageHandler
    {
        void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence);
        void OnMessage(IMessage message);
    }
}
