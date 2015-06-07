using Bender.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bender.Persistence
{
    internal class JsonKeyValuePersistence : IKeyValuePersistence
    {
        private readonly string filePath;
        private readonly ConcurrentDictionary<string, string> storage;
        private readonly object saveLock = new object();

        public JsonKeyValuePersistence(IConfiguration config)
        {
            this.filePath = Path.Combine(config.ModulesDirectoryPath, "storage.json");

            if (File.Exists(filePath))
            {
                this.storage = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(File.ReadAllText(filePath));
            }
            else
            {
                this.storage = new ConcurrentDictionary<string, string>();

                this.Save();
            }
        }

        public string Get(string key)
        {
            return this.storage[key];
        }

        public void Set(string key, string value)
        {
            this.storage[key] = value;
            
            this.Save();
        }

        private void Save()
        {
            lock(this.saveLock)
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(this.storage));
            }
        }
    }
}
