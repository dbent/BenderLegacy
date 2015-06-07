using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    internal class Echo : IModule
    {
        private static readonly Regex Regex = new Regex(@"^\s*(?:say|echo)\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private IBackend _backend;

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _backend = backend;
        }

        public void OnMessage(IMessage message)
        {
            if (message.IsRelevant)
            {
                var match = Regex.Match(message.Body);

                if (match.Success)
                {
                     _backend.SendMessageAsync(message.ReplyTo, match.Groups[1].Value);
                }
            }
        }
    }
}
