using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    internal class Extend : IModule
    {
        private static readonly IList<string> Confirmations = new List<string> { "OK!", "Will do!", "Roger that!", "Sure!", "Okey dokey!" };
        private IConfiguration _configuration;
        private IBackend _backend;
        private IKeyValuePersistence _persistence;
        private Regex _regexGetDll;
        private Regex _regexEnableModule;
        private Regex _regexDisableModule;
        private Random _random;

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _configuration = config;
            _backend = backend;
            _persistence = persistence;
            _random = new Random();
            _regexGetDll = new Regex(@"^\s*load library\s+(.+?)\s*$", RegexOptions.IgnoreCase);
            _regexEnableModule = new Regex(@"^\s*enable module\s+(.+?)\s*$", RegexOptions.IgnoreCase);
            _regexDisableModule = new Regex(@"^\s*disable module\s+(.+?)\s*$", RegexOptions.IgnoreCase);
        }

        public async void OnMessage(IMessage message)
        {
            if (message.IsRelevant)
            {
                var match = _regexGetDll.Match(message.Body);
                if (match.Success)
                {
                    Exception failureException = null;
                    try
                    {
                        Uri remoteUri = new Uri(match.Groups[1].Value, UriKind.Absolute);
                        await DownloadDll(remoteUri, message.SenderName);
                        await _backend.SendMessageAsync(message.ReplyTo, GetRandomConfirmation());
                    }
                    catch (Exception ex)
                    {
                        failureException = ex;
                    }
                    if (failureException != null)
                    {
                        var apology = "I can't do that right now.  I'm sorry.";
                        if (!message.ReplyTo.Equals(message.SenderAddress))
                        {
                            await _backend.SendMessageAsync(message.ReplyTo, apology);
                            apology = "I'm sorry I couldn't help you just now.";
                        }
                        await _backend.SendMessageAsync(message.SenderAddress, apology + "  Here's the full exception message:\n\n" + failureException.Message);
                    }
                    return;
                }
                
                match = _regexEnableModule.Match(message.Body);
                if (match.Success)
                {
                    _configuration.EnableModule(match.Groups[1].Value, _backend, _persistence);
                    await _backend.SendMessageAsync(message.ReplyTo, GetRandomConfirmation());
                    return;
                }
                
                match = _regexDisableModule.Match(message.Body);
                if (match.Success)
                {
                    _configuration.DisableModule(match.Groups[1].Value);
                    await _backend.SendMessageAsync(message.ReplyTo, GetRandomConfirmation());
                }
            }
        }

        private async Task DownloadDll(Uri remoteUri, string from)
        {
            var response = await new HttpClient().GetAsync(remoteUri);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsByteArrayAsync();
            var path = Path.Combine(_configuration.ModulesDirectoryPath, from.Replace(' ', '_') + Guid.NewGuid() + ".dll");
            File.WriteAllBytes(path, content);
            //await File.WriteAllBytesAsync(path, content);
        }

        private string GetRandomConfirmation()
        {
            return Confirmations[_random.Next(Confirmations.Count)];
        }
    }
}
