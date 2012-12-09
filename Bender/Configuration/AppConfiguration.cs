using Bender.Common;
using Bender.Module;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bender.Configuration
{
    internal class AppConfiguration : IConfiguration
    {
        private ModuleResolver moduleResolver;
        private ISet<string> enabledModules;

        public string this[string key]
        {
            get
            {
                return ConfigurationManager.AppSettings[key];
            }
        }

        public string Name
        {
            get { return this[Constants.ConfigKey.Name]; }
        }

        public string Jid
        {
            get { return this[Constants.ConfigKey.XmppJid]; }
        }

        public string Password
        {
            get { return this[Constants.ConfigKey.XmppPassword]; }
        }

        public string ModulesDirectoryPath
        {
            get { return this[Constants.ConfigKey.ModulesDirectory]; }
        }

        public IEnumerable<string> Rooms
        {
            get
            {
                return Regex.Split(this[Constants.ConfigKey.XmppRooms], @"\s+");
            }
        }

        public IEnumerable<IModule> Modules
        {
            get
            {
                AssertModuleResolver();
                return moduleResolver.GetModules();
            }
        }

        public AppConfiguration()
        {
            enabledModules = new HashSet<string>(Regex.Split(this[Constants.ConfigKey.Modules], @"\s+"));
        }

        public void Start(IBackend backend)
        {
            foreach (var module in Modules)
            {
                module.OnStart(this, backend);
            }
        }

        public void EnableModule(string moduleName, IBackend backend)
        {
            enabledModules.Add(moduleName);
            AssertModuleResolver();
            moduleResolver.FilterModules();
            Start(backend);
            //TODO: Write this change to the configuration file
        }

        public void DisableModule(string moduleName)
        {
            enabledModules.Remove(moduleName);
            AssertModuleResolver();
            moduleResolver.FilterModules();
            //TODO: Write this change to the configuration file
        }

        private void AssertModuleResolver()
        {
            if (moduleResolver == null)
            {
                DirectoryInfo modulesDirectory = null;
                if (!string.IsNullOrWhiteSpace(ModulesDirectoryPath))
                {
                    modulesDirectory = new DirectoryInfo(ModulesDirectoryPath);
                }
                moduleResolver = new ModuleResolver(enabledModules, modulesDirectory);
            }
        }
    }
}
