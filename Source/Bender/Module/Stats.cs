using Bender.Configuration;
using Bender.Persistence;
using Bent.Common.Extensions;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    public class Stats : IModule
    {
        private static Regex regexUptime = new Regex(@"^\s*uptime\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private IConfiguration config;
        private IBackend backend;

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            this.config = config;
            this.backend = backend;
        }

        public void OnMessage(IMessage message)
        {
            if (message.IsRelevant)
            {
                TryUptime(message);
            }
        }

        private void TryUptime(IMessage message)
        {
            var match = regexUptime.Match(message.Body);
            if (match.Success)
            {
                var startTime = Process.GetCurrentProcess().StartTime;
                this.backend.SendMessageAsync(message.ReplyTo, @"I have been running since {0} ({1}).".FormatWith(startTime, DateTime.Now - startTime));
            }
        }
    }
}
