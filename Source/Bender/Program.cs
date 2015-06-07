using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bender.Backend;
using Bender.Backend.Xmpp;
using Bender.Backend.Xmpp.Bend;
using Bender.Configuration;
using Bender.Persistence;

namespace Bender
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var config = new AppConfiguration();

            var bot = new Bot(config, new BendBackend(config), new JsonKeyValuePersistence(config));

            bot.RunAsync().Wait();
        }
    }
}
