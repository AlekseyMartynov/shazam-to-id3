using System;
using System.Net;
using System.Text.RegularExpressions;

class SpotifyHtmlReader {

    public static TrackMetadata Read(string id) {
        var ua = "curl/7.55.1";
        using(var web = new WebClient()) {
            var result = new TrackMetadata();

            var trackUrl = "https://open.spotify.com/track/" + id;
            web.Headers[HttpRequestHeader.UserAgent] = ua;
            var trackHtml = web.DownloadString(trackUrl);

            var albumUrl = ReadMeta(trackHtml, "music:album");
            web.Headers[HttpRequestHeader.UserAgent] = ua;
            var albumHtml = web.DownloadString(albumUrl);

            var coverUrl = ReadMeta(trackHtml, "og:image");

            result.Title = ReadMeta(trackHtml, "og:title");
            result.Artists = new[] { ReadMeta(trackHtml, "twitter:audio:artist_name") };
            result.Album = ReadMeta(albumHtml, "og:title");
            result.TrackNumber = Convert.ToUInt32(ReadMeta(trackHtml, "music:album:track"));
            result.Year = Convert.ToUInt32(ReadMeta(trackHtml, "music:release_date")?.Substring(0, 4));
            result.SourceUrl = trackUrl;
            web.Headers[HttpRequestHeader.UserAgent] = ua;
            result.Artwork = web.DownloadData(coverUrl);

            return result;
        }

    }

    static string ReadMeta(string html, string prop) {
        var m = Regex.Match(
            html,
            @"<meta\s+property=""" + Regex.Escape(prop)  +  @"""\s+content=""(.+?)""",
            RegexOptions.Singleline | RegexOptions.IgnoreCase
        );

        if(!m.Success)
            return null;

        return WebUtility.HtmlDecode(m.Groups[1].Value);
    }

}
