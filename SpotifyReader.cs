using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;

class SpotifyReader {

    public static TrackMetadata Read(string id) {
        using(var web = new WebClient()) {
            web.Headers[HttpRequestHeader.Accept] = "application/json";

            // Get temporary token at https://developer.spotify.com/console/get-track/
            web.Headers[HttpRequestHeader.Authorization] = "Bearer " + ReadToken();

            web.Proxy = new WebProxy();

            var json = web.DownloadString("https://api.spotify.com/v1/tracks/" + id);
            var jObj = JsonConvert.DeserializeObject<JObject>(json);

            var jAlbum = jObj["album"];

            var result = new TrackMetadata {
                Title = (string)jObj["name"],
                Artists = jObj["artists"].Select(a => (string)a["name"]).Where(a => a != "Various Artists").ToArray(),
                Album = (string)jAlbum["name"],
                TrackNumber = (uint)jObj["track_number"],
                Year = UInt32.Parse(((string)jAlbum["release_date"]).Substring(0, 4)),
                SourceUrl = "spotify:track:" + id
            };

            var artworkUrl = jAlbum["images"]
                .OrderByDescending(i => (int)i["height"])
                .Select(i => (string)i["url"])
                .FirstOrDefault();

            if(artworkUrl != null)
                result.Artwork = web.DownloadData(artworkUrl);

            return result;
        }
    }

    static string ReadToken() {
        var filename = "SPOTIFY_TOKEN";

        if(!File.Exists(filename)) {
            filename = Path.Combine(
                Path.GetDirectoryName(typeof(Program).Assembly.Location),
                "../../..",
                filename
            );
        }

        return File.ReadAllText(filename).Trim();
    }

}
