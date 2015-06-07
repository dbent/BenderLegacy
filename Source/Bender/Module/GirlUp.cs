using System;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;
using Newtonsoft.Json.Linq;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    public class GirlUp : IModule
    {
        private static readonly Regex Regex = new Regex(@"girl\s+up\s+the\s+chat", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        private readonly Random _random = new Random();

        private IConfiguration _config;
        private IBackend _backend;

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _config = config;
            _backend = backend;
        }

        public async void OnMessage(IMessage message)
        {
            try
            {
                if (!message.IsFromMyself && !message.IsHistorical)
                {
                    var match = Regex.Match(message.FullBody);

                    if (match.Success)
                    {
                        await _backend.SendMessageAsync(message.ReplyTo, await GetRandomProgrammerGoslingUrlAsync() + " " + new string('~', 3 + _random.Next(11)));
                    }
                }
            }
            catch(Exception e)
            {
                Console.Error.WriteLine(e); // TODO: Handle better
            }
        }

        private async Task<string> GetRandomProgrammerGoslingUrlAsync()
        {
            var apiKey = _config[Constants.ConfigKey.TumblrApiKey];

            var infoUrl = "http://api.tumblr.com/v2/blog/programmerryangosling.tumblr.com/info?api_key=" + HttpUtility.UrlEncode(apiKey);

            var infoResponse = await new HttpClient().GetAsync(infoUrl);
            infoResponse.EnsureSuccessStatusCode();

            int posts = (JObject.Parse(await infoResponse.Content.ReadAsStringAsync()) as dynamic).response.blog.posts;

            var imageUrl = "http://api.tumblr.com/v2/blog/programmerryangosling.tumblr.com/posts?limit=1&api_key=" + HttpUtility.UrlEncode(apiKey) + "&offset=" + _random.Next(posts);
            var imageResponse = await new HttpClient().GetAsync(imageUrl);
            imageResponse.EnsureSuccessStatusCode();

            var image = (JObject.Parse(await imageResponse.Content.ReadAsStringAsync()) as dynamic);

            return image.response.posts[0].photos[0].original_size.url;
        }
    }
}
