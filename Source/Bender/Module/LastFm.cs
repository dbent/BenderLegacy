using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bender.Apis.LastFm;
using Bender.Configuration;
using Bender.Interfaces;
using Bender.Persistence;

namespace Bender.Module
{
    // TODO: break out classes

    [Export(typeof(IModule))]
    public class LastFm : IModule
    {
        #region Regular Expressions

        private static readonly Regex MusicRegex = new Regex(@"^\s*music\s+(.+?)\s*\.?\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex SimilarTrackRegex = new Regex(@"^\s*similar\s+to\s+""(.+)""\s+by\s+(.+)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex SimilarArtistRegex = new Regex(@"^\s*similar\s+to\s+(.+)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex DiscoveryChainArtistRegex = new Regex(@"^\s*discovery\s+(.+)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex DiscoveryChainTrackRegex = new Regex(@"^\s*discovery\s+""(.+)""\s+by\s+(.+)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex HypedTracksRegex = new Regex(@"^\s*hyped\s+tracks\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex TopTracksRegex = new Regex(@"^\s*top\s+tracks\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex TopTracksByArtistRegex = new Regex(@"^\s*top\s+tracks\s+by\s+(.+)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex TopAlbumsByArtistRegex = new Regex(@"^\s*top\s+albums\s+by\s+(.+)\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex HelpRegex = new Regex(@"^\s*help\s*$", RegexOptions.IgnoreCase);

        #endregion

        #region Fields

        private IBackend _backend;
        private IConfiguration _config;

        #endregion

        #region IModule Members

        public void OnStart(IConfiguration config, IBackend backend, IKeyValuePersistence persistence)
        {
            _config = config;
            _backend = backend;
        }

        public void OnMessage(IMessage message)
        {
            TestMusic(message);
        }

        #endregion

        #region Regex Tests

        private async void TestMusic(IMessage message)
        {
            try
            {
                if (message.IsRelevant && !message.IsHistorical)
                {
                    var musicMatch = MusicRegex.Match(message.Body);
                    var musicBody = musicMatch.Groups[1].Value;
                    if (musicMatch.Success)
                    {
                        if (await TestSimilarTracks    (message, musicBody)) return;
                        if (await TestSimilarArtists   (message, musicBody)) return;
                        if (await TestTrackDiscovery   (message, musicBody)) return;
                        if (await TestArtistDiscovery  (message, musicBody)) return;
                        if (await TestHypedTracks      (message, musicBody)) return;
                        if (await TestTopTracks        (message, musicBody)) return;
                        if (await TestTopTracksByArtist(message, musicBody)) return;
                        if (await TestTopAlbumsByArtist(message, musicBody)) return;
                        if (await TestHelp             (message, musicBody)) return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task<bool> TestSimilarTracks(IMessage message, string musicBody)
        {
            var similarTrackMatch = SimilarTrackRegex.Match(musicBody);
            if (similarTrackMatch.Success)
            {
                var track = similarTrackMatch.Groups[1].Value;
                var artist = similarTrackMatch.Groups[2].Value;
                var xml = await new LastFmClient(_config[Constants.ConfigKey.LastFmApiKey]).GetSimilarTracksAsync(artist, track);
                await _backend.SendMessageAsync(message.ReplyTo, LastFmResponse.CreateSimilarTracksResponse(xml));
                return true;
            }
            return false;
        }

        private async Task<bool> TestSimilarArtists(IMessage message, string body)
        {
            var similarArtistMatch = SimilarArtistRegex.Match(body);
            if (similarArtistMatch.Success)
            {
                var artist = similarArtistMatch.Groups[1].Value;
                var xml = await new LastFmClient(_config[Constants.ConfigKey.LastFmApiKey]).GetSimilarArtistsAsync(artist);
                await _backend.SendMessageAsync(message.ReplyTo, LastFmResponse.CreateSimilarArtistsResponse(xml));
                return true;
            }
            return false;
        }

        private async Task<bool> TestTrackDiscovery(IMessage message, string body)
        {
            var discoveryChainTrackMatch = DiscoveryChainTrackRegex.Match(body);
            if (discoveryChainTrackMatch.Success)
            {
                var track = discoveryChainTrackMatch.Groups[1].Value;
                var artist = discoveryChainTrackMatch.Groups[2].Value;
                await _backend.SendMessageAsync(message.ReplyTo, "Looking for cool stuff. Please be patient.");
                var discovered = await DiscoveryChainTrackLoop(artist, track, 10);
                await _backend.SendMessageAsync(message.ReplyTo, "Discovery chain:\r\n" + String.Join(" ->\r\n", discovered));
                return true;
            }
            return false;
        }

        private async Task<bool> TestArtistDiscovery(IMessage message, string body)
        {
            var discoveryChainArtistMatch = DiscoveryChainArtistRegex.Match(body);
            if (discoveryChainArtistMatch.Success)
            {
                var artist = discoveryChainArtistMatch.Groups[1].Value;
                await _backend.SendMessageAsync(message.ReplyTo, "Looking for cool stuff. Please be patient.");
                var discovered = await DiscoveryChainArtistLoop(artist, 10);
                await _backend.SendMessageAsync(message.ReplyTo, "Discovery chain: " + String.Join(" -> ", discovered));
                return true;
            }
            return false;
        }

        private async Task<bool> TestHypedTracks(IMessage message, string body)
        {
            if (HypedTracksRegex.Match(body).Success)
            {
                var xml = await new LastFmClient(_config[Constants.ConfigKey.LastFmApiKey]).GetHypedTracksAsync();
                await _backend.SendMessageAsync(message.ReplyTo, LastFmResponse.CreateHypedTracksResponse(xml));
                return true;
            }
            return false;
        }

        private async Task<bool> TestTopTracks(IMessage message, string body)
        {
            if (TopTracksRegex.Match(body).Success)
            {
                var xml = await new LastFmClient(_config[Constants.ConfigKey.LastFmApiKey]).GetTopTracksAsync();
                await _backend.SendMessageAsync(message.ReplyTo, LastFmResponse.CreateTopTracksResponse(xml));
                return true;
            }
            return false;
        }

        private async Task<bool> TestTopTracksByArtist(IMessage message, string body)
        {
            if (TopTracksByArtistRegex.Match(body).Success)
            {
                var artist = TopTracksByArtistRegex.Match(body).Groups[1].Value;
                var xml = await new LastFmClient(_config[Constants.ConfigKey.LastFmApiKey]).GetArtistTopTracksAsync(artist);
                await _backend.SendMessageAsync(message.ReplyTo, LastFmResponse.CreateTopTracksResponse(xml, artist));
                return true;
            }
            return false;
        }

        private async Task<bool> TestTopAlbumsByArtist(IMessage message, string body)
        {
            if (TopAlbumsByArtistRegex.Match(body).Success)
            {
                var artist = TopAlbumsByArtistRegex.Match(body).Groups[1].Value;
                var xml = await new LastFmClient(_config[Constants.ConfigKey.LastFmApiKey]).GetArtistTopAlbumsAsync(artist);
                await _backend.SendMessageAsync(message.ReplyTo, LastFmResponse.CreateTopAlbumsResponse(xml, artist, false, 10));
                return true;
            }
            return false;
        }

        private async Task<bool> TestHelp(IMessage message, string body)
        {
            var helpMatch = HelpRegex.Match(body);
            if (helpMatch.Success)
            {
                await _backend.SendMessageAsync(message.ReplyTo, LastFmResponse.CreateHelpResponse(_config.Name));
                return true;
            }
            return false;
        }

        #endregion

        #region Web Service Loops

        // TODO: prevent cycles
        private async Task<List<Track>> DiscoveryChainTrackLoop(string artist, string track, int iterations)
        {
            Debug.Assert(iterations <= 10);

            var discovered = new List<Track>();
            var originalTrackName = new Track(artist, track);
            for (var i = 0; i < iterations; i++)
            {
                var xml = await new LastFmClient(_config[Constants.ConfigKey.LastFmApiKey]).GetSimilarTracksAsync(originalTrackName.Artist, originalTrackName.TrackName);
                var similar = LastFmXmlParser.GetSimilarTracks(xml, out originalTrackName, true, 1);

                if (i == 0)
                {
                    discovered.Add(originalTrackName);
                }

                if (similar.Any())
                {
                    discovered.Add(similar.First());
                }
                else
                {
                    break;
                }
            }

            return discovered.ToList();
        }

        // TODO: prevent cycles
        private async Task<List<Artist>> DiscoveryChainArtistLoop(string artist, int iterations)
        {
            Debug.Assert(iterations <= 10);

            var discovered = new List<Artist>();

            var originalArtistName = artist;
            for (var i = 0; i < iterations; i++)
            {
                var xml = await new LastFmClient(_config[Constants.ConfigKey.LastFmApiKey]).GetSimilarArtistsAsync(originalArtistName);
                var similar = LastFmXmlParser.GetSimilarArtistNames(xml, out originalArtistName, true, 1);

                if (i == 0)
                {
                    discovered.Add(new Artist(originalArtistName));
                }

                if (similar.Any())
                {
                    discovered.Add(similar.First());
                    originalArtistName = similar.First().Name;
                }
                else
                {
                    break;
                }
            }

            return discovered;
        }

        #endregion

        #region Private Classes

        private class Artist
        {
            public string Name { get; }

            public Artist(string name)
            {
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private class Track
        {
            public string Artist { get; }
            public string TrackName { get; }

            public Track(string artist, string trackName)
            {
                Artist = artist;
                TrackName = trackName;
            }

            public override string ToString()
            {
                return $"\"{TrackName}\" by {Artist}";
            }
        }

        private class Album
        {
            public string Artist { get; }
            public string AlbumName { get; }

            public Album(string artist, string albumName)
            {
                Artist = artist;
                AlbumName = albumName;
            }

            public override string ToString()
            {
                return $"\"{AlbumName}\" by {Artist}";
            }
        }

        private static class LastFmXmlParser
        {
            public static List<Artist> GetSimilarArtistNames(XDocument xml, out string originalArtistName, bool isRandomized = true, int limit = 10)
            {
                Debug.Assert(limit > 0);

                originalArtistName = xml
                    .Descendants("similarartists").First()
                    .Attribute("artist").Value;
                
                var r = new Random();

                return xml.Descendants("artist").OrderBy(x => isRandomized ? r.Next() : 0).Take(limit).Select(item => new Artist(item.Element("name").Value)).ToList();
            }

            public static List<Track> GetSimilarTracks(XDocument xml, out Track originalTrackName, bool isRandomized = false, int limit = 25)
            {
                Debug.Assert(limit > 0);

                var similarTracksElement = xml.Descendants("similartracks").First();
                originalTrackName = new Track(
                    similarTracksElement.Attribute("artist").Value,
                    similarTracksElement.Attribute("track").Value
                );

                return GetTracks(xml, isRandomized, limit);
            }

            public static List<Track> GetTracks(XDocument xml, bool isRandomized = true, int limit = 10)
            {
                var r = new Random();

                return xml.Descendants("track").OrderBy(x => isRandomized ? r.Next() : 0).Take(limit).Select(item => new Track(item.Element("artist").Element("name").Value, item.Element("name").Value)).ToList();
            }

            public static List<Album> GetAlbums(XDocument xml, bool isRandomized = false, int limit = 10)
            {
                var r = new Random();

                return xml.Descendants("album").OrderBy(x => isRandomized ? r.Next() : 0).Take(limit).Select(item => new Album(item.Element("artist").Element("name").Value, item.Element("name").Value)).ToList();
            }
        }

        private static class LastFmResponse
        {
            public static string CreateSimilarArtistsResponse(XDocument xml, bool isRandomized = true, int limit = 10)
            {
                string originalArtistName;
                var similarArtists = LastFmXmlParser.GetSimilarArtistNames(xml, out originalArtistName, isRandomized, limit);

                var response = new StringBuilder();
                response
                    .Append("Similar artists to ")
                    .Append(originalArtistName)
                    .Append(": ")
                    .Append(String.Join(", ", similarArtists))
                    .Append(".");

                return response.ToString();
            }

            public static string CreateSimilarTracksResponse(XDocument xml, bool isRandomized = false, int limit = 25)
            {
                Track originalTrack;
                var similarTrackNames = LastFmXmlParser.GetSimilarTracks(xml, out originalTrack, isRandomized, limit);

                var response = new StringBuilder();
                response
                    .Append("Similar songs to ")
                    .Append(originalTrack)
                    .Append(":\r\n")
                    .Append(string.Join("\r\n", similarTrackNames));

                return response.ToString();
            }

            public static string CreateHypedTracksResponse(XDocument xml, bool isRandomized = false, int limit = 25)
            {
                var trackNames = LastFmXmlParser.GetTracks(xml, isRandomized, limit);

                var response = new StringBuilder();
                response
                    .Append("Hyped tracks:\r\n")
                    .Append(string.Join("\r\n", trackNames));

                return response.ToString();
            }

            public static string CreateTopTracksResponse(XDocument xml, string artist = null, bool isRandomized = false, int limit = 25)
            {
                var trackNames = LastFmXmlParser.GetTracks(xml, isRandomized, limit);

                var response = new StringBuilder();
                response
                    .Append("Top tracks")
                    .Append(string.IsNullOrEmpty(artist) ? string.Empty : " by " + artist)
                    .Append(":\r\n")
                    .Append(string.Join("\r\n", trackNames));

                return response.ToString();
            }

            public static string CreateTopAlbumsResponse(XDocument xml, string artist = null, bool isRandomized = false, int limit = 25)
            {
                var albumNames = LastFmXmlParser.GetAlbums(xml, isRandomized, limit);

                var response = new StringBuilder();
                response
                    .Append("Top albums")
                    .Append(string.IsNullOrEmpty(artist) ? string.Empty : " by " + artist)
                    .Append(":\r\n")
                    .Append(string.Join("\r\n", albumNames));

                return response.ToString();
            }

            public static string CreateHelpResponse(string botName)
            {
                var response = new StringBuilder();

                // TODO: This should really be a string constant
                response.AppendLine();
                response.AppendLine(botName + " music help");
                response.AppendLine("    The help text you are currently viewing.");
                response.AppendLine();
                response.AppendLine(botName + " music similar to Rebecca Black");
                response.AppendLine("    Returns a randomized list of artists that are similar to Rebecca Black.");
                response.AppendLine();
                response.AppendLine(botName + " music similar to \"Whip My Hair\" by Willow Smith");
                response.AppendLine("    Returns a list of songs that are similar to \"Whip My Hair\" by Willow Smith, sorted by relevance.");
                response.AppendLine();
                response.AppendLine(botName + " music discovery Miley Cyrus");
                response.AppendLine("    Returns a discovery chain of artists, beginning with Miley Cyrus.");
                response.AppendLine();
                response.AppendLine(botName + " music discovery \"Ice Ice Baby\" by Vanilla Ice");
                response.AppendLine("    Returns a discovery chain of songs, beginning with \"Ice Ice Baby\" by Vanilla Ice.");
                response.AppendLine();
                response.AppendLine(botName + " music top tracks by Taylor Swift");
                response.AppendLine("    Returns the most popular songs by Taylor Swift.");
                response.AppendLine();
                response.AppendLine(botName + " music top albums by The Beatles");
                response.AppendLine("    Returns the most popular albums by The Beatles.");
                response.AppendLine();
                response.AppendLine(botName + " music top tracks");
                response.AppendLine("    Returns the most popular songs on Last.fm.");
                response.AppendLine();
                response.AppendLine(botName + " music hyped tracks");
                response.AppendLine("    Returns the fastest rising songs on Last.fm.");
                response.AppendLine();
                response.AppendLine();
                response.AppendLine("More cool features coming soon!");

                return response.ToString();
            }
        }

        #endregion
    }
}
