using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using Bender.Interfaces;
using Bender.Module;
using Bender.Persistence;

namespace Bender.Configuration
{
    internal class AppConfiguration : IConfiguration
    {
        private ModuleResolver _moduleResolver;
        private readonly ISet<string> _enabledModules;

        public string this[string key] => ConfigurationManager.AppSettings[key];
        public string Name => this[Constants.ConfigKey.Name];
        public string Jid => this[Constants.ConfigKey.XmppJid];
        public string Password => this[Constants.ConfigKey.XmppPassword];
        public string ModulesDirectoryPath => this[Constants.ConfigKey.ModulesDirectory];
        public IEnumerable<string> Rooms => Regex.Split(this[Constants.ConfigKey.XmppRooms], @"\s+");

        public IEnumerable<IModule> Modules
        {
            get
            {
                AssertModuleResolver();
                return _moduleResolver.GetModules();
            }
        }

        public AppConfiguration()
        {
            _enabledModules = new HashSet<string>(Regex.Split(this[Constants.ConfigKey.Modules], @"\s+"));
        }

        public void Start(IBackend backend, IKeyValuePersistence persistence)
        {
            foreach (var module in Modules)
            {
                module.OnStart(this, backend, persistence);
            }
        }

        public void EnableModule(string moduleName, IBackend backend, IKeyValuePersistence persistence)
        {
            _enabledModules.Add(moduleName);
            AssertModuleResolver();
            _moduleResolver.FilterModules();
            Start(backend, persistence);
            //TODO: Write this change to the configuration file
        }

        public void DisableModule(string moduleName)
        {
            _enabledModules.Remove(moduleName);
            AssertModuleResolver();
            _moduleResolver.FilterModules();
            //TODO: Write this change to the configuration file
        }

        private void AssertModuleResolver()
        {
            if (_moduleResolver == null)
            {
                DirectoryInfo modulesDirectory = null;
                if (!string.IsNullOrWhiteSpace(ModulesDirectoryPath))
                {
                    modulesDirectory = new DirectoryInfo(ModulesDirectoryPath);
                }
                _moduleResolver = new ModuleResolver(_enabledModules, modulesDirectory);
            }
        }
    }
}
