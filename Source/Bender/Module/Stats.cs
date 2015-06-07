using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Internal.Extensions;
using Bender.Persistence;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    public class Stats : IModule
    {
        private static readonly Regex RegexUptime = new Regex(@"^\s*uptime\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private IBackend _backend;

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _backend = backend;
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
            var match = RegexUptime.Match(message.Body);
            if (match.Success)
            {
                var startTime = Process.GetCurrentProcess().StartTime;
                _backend.SendMessageAsync(message.ReplyTo, @"I have been running since {0} ({1}).".FormatWith(startTime, DateTime.Now - startTime));
            }
        }
    }
}
