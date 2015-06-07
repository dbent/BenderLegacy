using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Web;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    public class Google : IModule
    {
        private static readonly Regex RegexGoogle = new Regex(@"^\s*google\s+(.+?)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex RegexLucky = new Regex(@"^\s*i'?m\s+feeling\s+lucky\s+(.+?)\s*$", RegexOptions.IgnoreCase);

        private IBackend _backend;

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _backend = backend;
        }

        public void OnMessage(IMessage message)
        {
            TestGoogle(message);
            TestLucky(message);
        }

        private void TestGoogle(IMessage message)
        {
            if (message.IsRelevant)
            {
                var match = RegexGoogle.Match(message.Body);

                if (match.Success)
                {
                    _backend.SendMessageAsync(message.ReplyTo, "http://lmgtfy.com/?q=" + HttpUtility.UrlEncode(match.Groups[1].Value));
                }
            }
        }

        private void TestLucky(IMessage message)
        {
            if (message.IsRelevant)
            {
                var match = RegexLucky.Match(message.Body);

                if (match.Success)
                {
                    _backend.SendMessageAsync(message.ReplyTo, "http://lmgtfy.com/?l=1&q=" + HttpUtility.UrlEncode(match.Groups[1].Value));
                }
            }
        }
    }
}
