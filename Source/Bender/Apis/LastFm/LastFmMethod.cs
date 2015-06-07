namespace Bender.Apis.LastFm
{
    public enum LastFmMethod
    {
        [LastFmMethodName("artist.getInfo")]
        ArtistGetInfo,
        [LastFmMethodName("artist.getSimilar")]
        ArtistGetSimilar,
        [LastFmMethodName("artist.getTopAlbums")]
        ArtistGetTopAlbums,
        [LastFmMethodName("artist.getTopTracks")]
        ArtistGetTopTracks,
        [LastFmMethodName("artist.search")]
        ArtistSearch,
        [LastFmMethodName("chart.getHypedArtists")]
        ChartGetHypedArtists,
        [LastFmMethodName("chart.getHypedTracks")]
        ChartGetHypedTracks,
        [LastFmMethodName("chart.getTopArtists")]
        ChartGetTopArtists,
        [LastFmMethodName("chart.getTopTracks")]
        ChartGetTopTracks,
        [LastFmMethodName("track.getSimilar")]
        TrackGetSimilar,
    }
}
