using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Bender.Apis.LastFm
{
    public class LastFmClient
    {
        private const int ResultLimit = 100;
        public const string url = "http://ws.audioscrobbler.com/2.0/";

        private string serviceUrl;

        public LastFmClient(string apiKey)
        {
            this.serviceUrl = url + "?api_key=" + apiKey;
        }

        public async Task<XDocument> GetHypedTracksAsync()
        {
            return await QueryGlobalAsync(LastFmMethod.Chart_GetHypedTracks);
        }

        public async Task<XDocument> GetTopTracksAsync()
        {
            return await QueryGlobalAsync(LastFmMethod.Chart_GetTopTracks);
        }

        public async Task<XDocument> GetArtistInfoAsync(string artist)
        {
            return await QueryArtistAsync(LastFmMethod.Artist_GetInfo, artist);
        }

        public async Task<XDocument> GetArtistTopTracksAsync(string artist)
        {
            return await QueryArtistAsync(LastFmMethod.Artist_GetTopTracks, artist);
        }

        public async Task<XDocument> GetArtistTopAlbumsAsync(string artist)
        {
            return await QueryArtistAsync(LastFmMethod.Artist_GetTopAlbums, artist);
        }

        public async Task<XDocument> GetSimilarArtistsAsync(string artist)
        {
            return await QueryArtistAsync(LastFmMethod.Artist_GetSimilar, artist);
        }

        public async Task<XDocument> GetSimilarTracksAsync(string artist, string track)
        {
            return await QueryTrackAsync(LastFmMethod.Track_GetSimilar, artist, track);
        }

        private async Task<XDocument> QueryArtistAsync(LastFmMethod method, string artist)
        {
            var query = new Dictionary<string, object>(){
                { "artist", artist }, { "limit", ResultLimit }
            };
            return await QueryAsync(GenerateUrl(method, query));
        }

        private async Task<XDocument> QueryTrackAsync(LastFmMethod method, string artist, string track)
        {
            var query = new Dictionary<string, object>(){
                { "artist", artist }, { "track", track }, { "limit", ResultLimit }
            };
            return await QueryAsync(GenerateUrl(method, query));
        }

        private async Task<XDocument> QueryGlobalAsync(LastFmMethod method)
        {
            var query = new Dictionary<string, object>(){
                { "limit", ResultLimit }
            };
            return await QueryAsync(GenerateUrl(method, new Dictionary<string, object>()));
        }

        public async Task<XDocument> QueryAsync(string url)
        {
            var response = await new HttpClient().GetAsync(url);
            response.EnsureSuccessStatusCode();
            return XDocument.Parse(await response.Content.ReadAsStringAsync());
        }

        private string GenerateUrl(
            LastFmMethod method, 
            Dictionary<string, object> query)
        {
            StringBuilder uri = new StringBuilder(this.serviceUrl);
            AppendQueryString(uri, "method", GetMethodName(method));
            foreach (var kvp in query)
            {
                AppendQueryString(uri, kvp.Key, kvp.Value.ToString());
            }
            return uri.ToString();
        }

        private string GetMethodName(LastFmMethod method)
        {
            var attribute = (LastFmMethodNameAttribute)method.GetType()
                .GetMember(method.ToString()).First()
                .GetCustomAttributes(typeof(LastFmMethodNameAttribute), false)
                .First();

            return attribute.Value;
        }

        private void AppendQueryString(StringBuilder uri, string param, string value)
        {
            Debug.Assert(!String.IsNullOrEmpty(param));

            if (!String.IsNullOrEmpty(value))
            {
                uri.AppendFormat("&{0}={1}", param, HttpUtility.UrlEncode(value));
            }
        }
    }
}
