using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using Bender.Module;

namespace Bender
{
    internal class ModuleResolver
    {
#pragma warning disable 0649
        [ImportMany(typeof(IModule), AllowRecomposition = true)]
        private IEnumerable<IModule> _importedModules;
#pragma warning restore 0649
        private readonly List<IModule> _loadedModules;
        private readonly IReadOnlyList<IModule> _readOnlyModules;
        private readonly IEnumerable<string> _moduleNames;
        private FileSystemWatcher _watcher;
        private DirectoryCatalog _dirCatalog;
        private readonly CompositionContainer container;

        public ModuleResolver(IEnumerable<string> moduleNames, DirectoryInfo directory = null)
        {
            var assCatalog = new AssemblyCatalog(typeof(Program).Assembly);
            var aggCatalog = new AggregateCatalog();
            aggCatalog.Catalogs.Add(assCatalog);
            if (directory != null)
            {
                WatchForAssemblies(directory, aggCatalog);
            }
            container = new CompositionContainer(aggCatalog);
            container.ComposeParts(this);

            _moduleNames = moduleNames;
            _loadedModules = new List<IModule>();
            FilterModules();
            _readOnlyModules = _loadedModules.AsReadOnly();
        }

        public IEnumerable<IModule> GetModules()
        {
            return _readOnlyModules;
        }
        
        private void WatchForAssemblies(DirectoryInfo directory, AggregateCatalog aggCatalog)
        {
            _dirCatalog = new DirectoryCatalog(directory.FullName);
            aggCatalog.Catalogs.Add(_dirCatalog);

            _watcher = new FileSystemWatcher(directory.FullName, "*.dll");
            _watcher.Created += OnAssemblyChanged;
            _watcher.Changed += OnAssemblyChanged;
            _watcher.Deleted += OnAssemblyChanged;
            _watcher.EnableRaisingEvents = true;
        }

        private void OnAssemblyChanged(object sender, FileSystemEventArgs e)
        {
            _dirCatalog.Refresh();
            FilterModules();
        }

        public void FilterModules()
        {
            foreach (var m in _importedModules)
            {
                if (_moduleNames.Contains(m.GetType().Name) && !_loadedModules.Contains(m))
                {
                    _loadedModules.Add(m);
                }
            }
        }
    }
}
