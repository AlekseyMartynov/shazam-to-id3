using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text.RegularExpressions;

class ShazamReader {

    public static TrackMetadata FromID(string id) {
        var url = "https://www.shazam.com/discovery/v4/-/-/web/-/track/" + id;
        using(var web = new WebClient()) {
            var meta = FromJson(web.DownloadString(url));
            meta.SourceUrl = "https://shz.am/t" + id;
            return meta;
        }
    }

    public static TrackMetadata FromJson(string json) {
        var root = JsonConvert.DeserializeObject<JObject>(json);
        var heading = root["heading"];
        var footnotes = (JArray)root["footnotes"];

        var result = new TrackMetadata {
            Title = (string)heading["title"],
            Artists = new[] { (string)heading["subtitle"] }
        };

        var imageUrl = (string)root["images"]?["default"];

        if(!String.IsNullOrEmpty(imageUrl)) {
            using(var web = new WebClient()) {
                try {
                    var noResizeUrl = Regex.Replace(imageUrl, @"_s\d+\.jpg$", "_s0.jpg");
                    result.Artwork = web.DownloadData(noResizeUrl);
                } catch(WebException) {
                    result.Artwork = web.DownloadData(imageUrl);
                }
            }
        }

        foreach(var item in footnotes) {
            var value = item["value"];
            switch((string)item["title"]) {
                case "Album":
                    result.Album = (string)value;
                    break;
                case "Label":
                    result.Label = (string)value;
                    break;
                case "Released":
                    result.Year = Convert.ToUInt32(value);
                    break;
            }
        }

        result.Genre = (string)root["advertising"]?["parameters"]?["gr"];

        return result;
    }

}
