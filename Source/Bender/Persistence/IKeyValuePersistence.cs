using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bender.Persistence
{
    public interface IKeyValuePersistence
    {
        string Get(string key);
        void Set(string key, string value);
    }
}
