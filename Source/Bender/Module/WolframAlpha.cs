using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bender.Apis.WolframAlpha;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    public class WolframAlpha : IModule
    {
        private static readonly Regex Regex = new Regex(@"\?\s*$", RegexOptions.IgnoreCase);

        private IConfiguration _config;
        private IBackend _backend;

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _config = config;
            _backend = backend;
        }

        public async void OnMessage(IMessage message)
        {
            if (message.IsRelevant)
            {
                if (Regex.IsMatch(message.Body))
                {
                    string answer = null;

                    var response = await new WolframAlphaClient(_config[Constants.ConfigKey.WolframAlphaApiKey]).QueryAsync(message.Body, Format.PlainText);

                    var resultPods = response.Descendants("pod").Where(i => string.Equals((string)i.Attribute("id"), "result", StringComparison.OrdinalIgnoreCase));
                    if (resultPods.Any())
                    {
                        var primary = resultPods.FirstOrDefault(i => string.Equals((string)i.Attribute("primary"), "true", StringComparison.OrdinalIgnoreCase));
                        if (primary != null)
                        {
                            var plaintext = primary.Descendants("plaintext").FirstOrDefault();
                            
                            if (plaintext != null)
                                answer = plaintext.Value;
                        }
                        else
                        {
                            var plaintext = resultPods.First().Descendants("plaintext").FirstOrDefault();

                            if (plaintext != null)
                                answer = plaintext.Value;
                        }
                    }
                    else
                    {
                        var notableFacts = response.Descendants("pod").Where(i => ((string)i.Attribute("id")).StartsWith("NotableFacts", StringComparison.OrdinalIgnoreCase));

                        if (notableFacts.Any())
                        {
                            var plaintext = notableFacts.First().Descendants("plaintext").FirstOrDefault();

                            if (plaintext != null)
                                answer = plaintext.Value;
                        }
                        else
                        {
                            var basicInformation = response.Descendants("pod").Where(i => ((string)i.Attribute("id")).StartsWith("BasicInformation", StringComparison.OrdinalIgnoreCase));

                            if (basicInformation.Any())
                            {
                                var plaintext = basicInformation.First().Descendants("plaintext").FirstOrDefault();

                                if (plaintext != null)
                                    answer = plaintext.Value;
                            }
                            else
                            {
                                // hail mary
                                var all = new StringBuilder();
                                foreach (var plaintext in response.Descendants("pod").Where(i => !string.Equals((string)i.Attribute("id"), "input", StringComparison.OrdinalIgnoreCase)).SelectMany(i => i.Descendants("plaintext")))
                                {
                                    all.AppendLine(plaintext.Value);
                                }

                                if (all.Length > 0)
                                    answer = all.ToString();
                            }
                        }                        
                    }

                    if(answer != null)
                    {
                        await _backend.SendMessageAsync(message.ReplyTo, answer);
                    }
                    else
                    {
                        await _backend.SendMessageAsync(message.ReplyTo, @"/me ¯\_(ツ)_/¯");
                    }
                }
            }
        }
    }
}
