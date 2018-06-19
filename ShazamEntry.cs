using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text.RegularExpressions;

class ShazamEntry {
    public string Title;
    public string Artist;
    public byte[] Artwork;
    public string Album;
    public string Label;
    public uint? Year;
    public string Genre;

    public static ShazamEntry FromID(string id) {
        var url = "https://www.shazam.com/discovery/v4/-/-/web/-/track/" + id;
        using(var web = new WebClient()) {
            return FromJson(web.DownloadString(url));
        }
    }

    public static ShazamEntry FromJson(string json) {
        var root = JsonConvert.DeserializeObject<JObject>(json);
        var heading = root["heading"];
        var footnotes = (JArray)root["footnotes"];

        var result = new ShazamEntry {
            Title = (string)heading["title"],
            Artist = (string)heading["subtitle"]
        };

        var imageUrl = (string)root["images"]?["default"];

        if(!String.IsNullOrEmpty(imageUrl)) {
            imageUrl = Regex.Replace(imageUrl, @"_s\d+\.jpg$", "_s0.jpg");
            using(var web = new WebClient()) {
                result.Artwork = web.DownloadData(imageUrl);
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
