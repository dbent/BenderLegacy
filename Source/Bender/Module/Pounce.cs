using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;

namespace Bender.Module
{
    // TODO: needs to support XMPP presence: i.e. when a user joins a room or their status becomes available
    [Export(typeof(IModule))]
    public class Pounce : IModule
    {
        private static readonly Regex Regex = new Regex(@"^\s*tell\s+(.+?)\s+that\s+(.+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly IList<string> Confirmations = new List<string> { "OK!", "Will do!", "Roger that!", "Sure!", "Okey dokey!" };

        private readonly Random _random = new Random();

        // TODO: need to store this permanently
        private readonly ConcurrentDictionary<string, ConcurrentQueue<Tuple<string, string>>> _messages = new ConcurrentDictionary<string, ConcurrentQueue<Tuple<string, string>>>(StringComparer.OrdinalIgnoreCase);        

        private IBackend _backend;
        private IConfiguration _config;

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _config = config;
            _backend = backend;
        }

        public void OnMessage(IMessage message)
        {
            CheckForPounces(message);
            CheckForNewPounce(message);
        }

        private void CheckForPounces(IMessage message)
        {
            if(!message.IsFromMyself && !message.IsHistorical)
            {
                if(_messages.ContainsKey(message.SenderName) && _messages[message.SenderName].Any())
                {
                    var pounces = _messages[message.SenderName].ToList();
                    _messages[message.SenderName] = new ConcurrentQueue<Tuple<string, string>>();

                    _backend.SendMessageAsync(message.ReplyTo,
                        $"Welcome back {message.SenderName}! {pounces.Select(i => $@"{i.Item1} said, ""{i.Item2}""").Aggregate((i, j) => $"{i} and {j}")}.");
                }
            }
        }

        private void CheckForNewPounce(IMessage message)
        {
            if(message.IsRelevant)
            {
                var match = Regex.Match(message.Body);

                if (match.Success)
                {
                    if (message.IsPrivate)
                    {
                        _backend.SendMessageAsync(message.ReplyTo, "This isn't a group chat!");
                    }
                    else
                    {
                        var target = match.Groups[1].Value;
                        var msg = match.Groups[2].Value;

                        if (target.Equals(_config.Name, StringComparison.OrdinalIgnoreCase) || target.Equals(message.SenderName, StringComparison.OrdinalIgnoreCase))
                        {
                            _backend.SendMessageAsync(message.ReplyTo, "O_o?");
                        }
                        else
                        {
                            _backend.SendMessageAsync(message.ReplyTo, GetRandomConfirmation());

                            if (!_messages.ContainsKey(target))
                            {
                                _messages[target] = new ConcurrentQueue<Tuple<string, string>>();
                            }

                            _messages[target].Enqueue(Tuple.Create(message.SenderName, msg));
                        }
                    }
                }
            }
        }

        private string GetRandomConfirmation()
        {
            return Confirmations[_random.Next(Confirmations.Count)];
        }
    }
}
