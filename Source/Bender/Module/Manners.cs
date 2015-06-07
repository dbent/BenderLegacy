using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    public class Manners : IModule
    {
        private static readonly List<string> Phrases = new List<string> { "No problem, {0}.", "You're welcome, {0}.", "Happy to help, {0}." };

        private readonly Random _random = new Random();

        private IBackend _backend;
        private Regex _regex;

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _backend = backend;
            _regex = new Regex($@"(thank.*?|^\s*ty,?(\s+.*)?)\s+{config.Name}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        public void OnMessage(IMessage message)
        {
            if(!message.IsFromMyself && !message.IsHistorical)
            {
                if(_regex.IsMatch(message.FullBody))
                {
                    _backend.SendMessageAsync(message.ReplyTo, string.Format(Phrases[_random.Next(Phrases.Count)], message.SenderName));
                }
            }
        }
    }
}
