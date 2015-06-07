using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    class Slap : IModule
    {
        private static readonly Regex Regex = new Regex(@"^\s*slap\s+(.+?)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex RegexJenna = new Regex("nice.+but", RegexOptions.IgnoreCase);

        private IBackend _backend;

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _backend = backend;
        }

        public void OnMessage(IMessage message)
        {
            Normal(message);
            Jenna(message);
        }

        private void Normal(IMessage message)
        {
            if (message.IsRelevant)
            {
                var match = Regex.Match(message.Body);

                if (match.Success)
                {
                    var target = match.Groups[1].Value;

                    _backend.SendMessageAsync(message.ReplyTo,
                        target.ToLowerInvariant().Contains("dwayne")
                            ? $"/me turns around and slaps {message.SenderName} with a large trout!"
                            : $"/me slaps {target} with a large trout!");
                }
            }
        }

        private void Jenna(IMessage message)
        {
            if(!message.IsFromMyself && !message.IsHistorical)
            {
                if(message.SenderName.ToLowerInvariant().StartsWith("jenna") || message.SenderName.ToLowerInvariant().StartsWith("jmh"))
                {
                    if(RegexJenna.IsMatch(message.FullBody))
                    {
                        _backend.SendMessageAsync(message.ReplyTo, "Jenna! (╯°□°）╯︵ ┻━┻");
                    }
                }
            }
        }
    }
}
