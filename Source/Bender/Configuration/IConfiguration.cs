using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bender.Backend.Xmpp;
using Bender.Module;
using Bender.Persistence;

namespace Bender.Configuration
{
    public interface IConfiguration
    {
        string this[string key] { get; }

        string Name { get; }
        string Jid { get; }
        string Password { get; }
        string ModulesDirectoryPath { get; }
        IEnumerable<string> Rooms { get; }
        IEnumerable<IModule> Modules { get; }

        void Start(IBackend backend, IKeyValuePersistence persistence);

        void EnableModule(string moduleName, IBackend backend, IKeyValuePersistence persistence);
        void DisableModule(string moduleName);
    }
}
