using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    public class Reddit : IModule
    {
        private static readonly Regex Regex = new Regex(@"^\s*reddit(\s+(.+))?\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex UltLinkRegex = new Regex(@"<br/>\s<a href=""(.+)"">\[link\]", RegexOptions.IgnoreCase);

        private readonly Dictionary<string, HashSet<string>> _seenLinks = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase); // TODO: persist

        private IBackend _backend;

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _backend = backend;
        }

        public async void OnMessage(IMessage message)
        {
            try 
            {
                if (message.IsRelevant)
                {
                    var match = Regex.Match(message.Body);
                    if (match.Success)
                    {
                        var subreddit = match.Groups[2].Value;
                        var url = (subreddit == string.Empty) ? "http://www.reddit.com/.rss" :
                            $"http://www.reddit.com/r/{subreddit}.rss";

                        var response = await new HttpClient().GetAsync(url);
                        if (!string.IsNullOrEmpty(subreddit) && (
                                response.StatusCode == HttpStatusCode.NotFound ||
                                !string.Equals(response.RequestMessage.RequestUri.ToString(), url, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            await _backend.SendMessageAsync(message.ReplyTo, "Sorry, couldn't find your subreddit. :-/");
                            return;
                        }
                        response.EnsureSuccessStatusCode();

                        var body = await response.Content.ReadAsStringAsync();
                        var xml = XDocument.Parse(body);

                        if (!_seenLinks.ContainsKey(subreddit))
                        {
                            _seenLinks[subreddit] = new HashSet<string>();
                        }

                        var messages = new List<string>();

                        foreach (var item in xml.Descendants("item"))
                        {
                            var titleEl = item.Elements("title").FirstOrDefault();
                            var linkEl = item.Elements("link").FirstOrDefault();
                            var descEl = item.Elements("description").FirstOrDefault();

                            if (titleEl != null && linkEl != null && descEl != null)
                            {
                                var title = titleEl.Value;
                                var link = linkEl.Value;
                                var ultLink = GetUltimateLink(descEl.Value);

                                if (!_seenLinks[subreddit].Contains(link))
                                {
                                    _seenLinks[subreddit].Add(link);

                                    messages.Add(GetMessage(title, link, ultLink));
                                }
                            }
                        }

                        if (messages.Any())
                        {
                            if (messages.Count > 3)
                            {
                                await _backend.SendMessageAsync(message.ReplyTo,
                                    $"There were {messages.Count} new stories, just going to give you the top 3.");
                            }

                            foreach (var m in messages.Take(3))
                            {
                                await _backend.SendMessageAsync(message.ReplyTo, m);
                            }
                        }
                        else
                        {
                            await _backend.SendMessageAsync(message.ReplyTo, "Sorry, nothing new. :-/");
                        }
                    }
                }
            }
            catch (Exception ex) 
            {
                Console.Error.WriteLine(ex);  // TODO: better exception handling
            }
        }

        private static string GetUltimateLink(string descriptionValue)
        {
            var match = UltLinkRegex.Match(descriptionValue);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string GetMessage(string title, string link, string ultLink)
        {
            return $"{title}\n{(string.IsNullOrEmpty(ultLink) ? link : ultLink)}";
        }
    }
}
