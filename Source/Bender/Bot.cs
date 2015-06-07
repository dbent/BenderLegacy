using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bender.Common;
using Bender.Configuration;
using Bender.Framework;
using Bender.Interfaces;
using Bender.Persistence;

namespace Bender
{
    public class Bot : IObserver<MessageData>
    {
        private readonly ManualResetEvent _done = new ManualResetEvent(false);

        private readonly IConfiguration _config;
        private readonly IBackend _backend;

        private readonly Regex _regexDirected;

        public Bot(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _config = config;
            _backend = backend;

            _regexDirected = new Regex($@"^\s*@?{_config.Name}(?:,\s*|:\s*|\s+)(.+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            _config.Start(_backend, persistence);
        }

        public async Task RunAsync()
        {
            using (_done)
            using (_backend.Subscribe(this))
            {
                await _backend.ConnectAsync();

                _done.WaitOne();
            }
        }

        void IObserver<MessageData>.OnCompleted()
        {
            _done.Set();
        }

        void IObserver<MessageData>.OnError(Exception error)
        {
            Console.Error.WriteLineAsync(error.ToString());
        }

        void IObserver<MessageData>.OnNext(MessageData value)
        {
            var matchDirected = _regexDirected.Match(value.Body);

            var message = new MessageImpl(value, matchDirected.Success ? matchDirected.Groups[1].Value : null,
                isAddressedAtMe: matchDirected.Success);

            Parallel.ForEach(_config.Modules, p =>
                {
                    try
                    {
                        p.OnMessage(message);
                    }
                    catch(Exception e)
                    {   // TODO: Bot: Handle plugin errors
                        Console.Error.WriteLineAsync(e.ToString());
                    }
                });
        }
    }
}
