using System.Collections.Concurrent;
using System.IO;
using Bender.Configuration;
using Newtonsoft.Json;

namespace Bender.Persistence
{
    internal class JsonKeyValuePersistence : IKeyValuePersistence
    {
        private readonly string _filePath;
        private readonly ConcurrentDictionary<string, string> _storage;
        private readonly object _saveLock = new object();

        public JsonKeyValuePersistence(IConfiguration config)
        {
            _filePath = Path.Combine(config.ModulesDirectoryPath, "storage.json");

            if (File.Exists(_filePath))
            {
                _storage = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(File.ReadAllText(_filePath));
            }
            else
            {
                _storage = new ConcurrentDictionary<string, string>();

                Save();
            }
        }

        public string Get(string key)
        {
            return _storage[key];
        }

        public void Set(string key, string value)
        {
            _storage[key] = value;
            
            Save();
        }

        private void Save()
        {
            lock(_saveLock)
            {
                File.WriteAllText(_filePath, JsonConvert.SerializeObject(_storage));
            }
        }
    }
}
