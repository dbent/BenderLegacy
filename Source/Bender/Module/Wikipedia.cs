using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Web;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    public class Wikipedia : IModule
    {
        private static readonly Regex RegexWiki = new Regex(@"^\s*(wiki|wikipedia)\s+(.+?)\s*$", RegexOptions.IgnoreCase);
        
        private Regex _regexAlias;
        private IBackend _backend;

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _backend = backend;
            if (!string.IsNullOrEmpty(config[Constants.ConfigKey.WikipediaAlias]))
            {
                _regexAlias = new Regex($@"^\s*{config[Constants.ConfigKey.WikipediaAlias]},?\s+(.+?)\s*$", RegexOptions.IgnoreCase);
            }
        }

        public void OnMessage(IMessage message)
        {
            TestWiki(message);
        }

        private void TestWiki(IMessage message)
        {
            if (!message.IsHistorical)
            {
                if (message.IsRelevant)
                {
                    var match = RegexWiki.Match(message.Body);
                    if (match.Success)
                    {
                        _backend.SendMessageAsync(message.ReplyTo, "http://en.wikipedia.org/wiki/" + HttpUtility.UrlEncode(match.Groups[2].Value.Replace(' ', '_')));
                    }
                }
                else if (_regexAlias != null)
                {
                    var match = _regexAlias.Match(message.FullBody);
                    if (match.Success)
                    {
                        _backend.SendMessageAsync(message.ReplyTo, "http://en.wikipedia.org/wiki/" + HttpUtility.UrlEncode(match.Groups[1].Value.Replace(' ', '_')));
                    }
                }
            }
        }
    }
}
