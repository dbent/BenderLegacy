using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Bender.Apis.WolframAlpha
{
    public class WolframAlphaClient
    {
        private readonly string _baseUrl;

        public WolframAlphaClient(string appId)
        {
            _baseUrl = "http://api.wolframalpha.com/v2/query?appid=" + HttpUtility.UrlEncode(appId);
        }

        public async Task<XDocument> QueryAsync(string query, params Format[] formats) // TODO: at least one format is required
        {
            var queryUrl = _baseUrl +
                "&format=" + formats.Select(i => HttpUtility.UrlEncode(Reference.GetFormatString(i))).Aggregate((i, j) => i + "," + j) +
                "&input=" + HttpUtility.UrlEncode(query);

            var response = await new HttpClient().GetAsync(queryUrl);
            response.EnsureSuccessStatusCode();

            return XDocument.Parse(await response.Content.ReadAsStringAsync());
        }
    }
}
