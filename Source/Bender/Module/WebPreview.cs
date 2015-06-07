using System;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;
using Newtonsoft.Json.Linq;

namespace Bender.Module
{
    /*
     * ENHANCE: Utilize OpenGraph/OEmbed and then fallback to DiffBot
     */
    [Export(typeof(IModule))]
    public class WebPreview : IModule
    {
        // TODO: this is a pretty restrictive regex
        private static readonly Regex Regex = new Regex(@"^\s*(https?://\S+)\s*", RegexOptions.IgnoreCase);
        
        private IBackend _backend;
        private string _apiEndPoint;

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _backend = backend;

            var token = config[Constants.ConfigKey.DiffBotApiKey];

            _apiEndPoint = "http://www.diffbot.com/api/article?summary&token=" + HttpUtility.UrlEncode(token);
        }

        public async void OnMessage(IMessage message)
        {
            try
            {
                if (!message.IsFromMyself && !message.IsHistorical)
                {
                    var tokens = Regex.Split(message.FullBody, @"\s+");

                    foreach (var token in tokens)
                    {
                        if (Regex.IsMatch(token))
                        {
                            var uri = new Uri(token);
                            if (uri.IsWellFormedOriginalString())
                            {
                                var reply = MakeReply(await QueryAsync(uri));

                                if (!string.IsNullOrWhiteSpace(reply))
                                {
                                    // TODO: gotta get new lines sorted out
                                    await _backend.SendMessageAsync(message.ReplyTo, reply);
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.Error.WriteLine(e); // TODO: handle better
            }
        }

        private async Task<DiffBotResponse> QueryAsync(Uri uri)
        {
            var queryUrl = _apiEndPoint + "&url=" + HttpUtility.UrlEncode(uri.ToString());

            var response = await new HttpClient().GetAsync(queryUrl);
            response.EnsureSuccessStatusCode();

            dynamic json = JObject.Parse(await response.Content.ReadAsStringAsync());

            return new DiffBotResponse((string)json.title, (string)json.author,(string)json.summary);
        }

        private static string MakeReply(DiffBotResponse response)
        {
            var reply = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(response.Title))
            {
                reply.AppendFormat(@"""{0}""", response.Title);
            }

            if(!string.IsNullOrWhiteSpace(response.Author))
            {
                reply.AppendFormat(" by {0}", response.Author);
            }

            if(!string.IsNullOrWhiteSpace(response.Summary))
            {
                // TODO: break on words
                reply.AppendFormat(" — {0}", response.Summary.Length <= 300 ? response.Summary : response.Summary.Substring(0, 300) + "…");
            }

            return reply.ToString();
        }

        private class DiffBotResponse
        {
            public string Title { get; }
            public string Author { get; }
            public string Summary { get; }

            public DiffBotResponse(string title, string author, string summary)
            {
                Title = title;
                Author = author;
                Summary = summary;
            }
        }
    }
}
