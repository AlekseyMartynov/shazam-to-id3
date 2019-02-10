using System;
using System.IO;
using System.Text.RegularExpressions;

class Program {

    static void Main(string[] args) {
        // args = new[] { "c:/temp/test.mp3", "379653703" };

        var mp3Path = args[0];
        var shid = args[1];

        var entry = ShazamEntry.FromID(shid);

        File.Copy(mp3Path, mp3Path + ".bak");

        using(var file = TagLib.File.Create(mp3Path)) {
            file.RemoveTags(TagLib.TagTypes.AllTags & ~TagLib.TagTypes.Id3v2);

            var id3 = (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2);
            id3.Clear();

            id3.Title = entry.Title;
            id3.Performers = entry.Artists;

            if(entry.Artwork != null) {
                id3.Pictures = new[] {
                    new TagLib.Id3v2.AttachedPictureFrame {
                        MimeType = entry.ArtworkMime,
                        Type = TagLib.PictureType.FrontCover,
                        Data = entry.Artwork,
                        TextEncoding = TagLib.StringType.Latin1
                    }
                };
            }

            if(!String.IsNullOrEmpty(entry.Album))
                id3.Album = entry.Album;

            if(!String.IsNullOrEmpty(entry.Label))
                id3.SetTextFrame("TPUB", entry.Label);

            if(entry.Year.HasValue)
                id3.Year = entry.Year.Value;

            if(!String.IsNullOrEmpty(entry.Genre))
                id3.Genres = new[] { entry.Genre };

            var url = new TagLib.ByteVector { 0, "Shazam", 0, "https://shz.am/t" + shid };
            id3.AddFrame(new TagLib.Id3v2.UnknownFrame("WXXX", url));

            file.Save();
        }

        if(Regex.IsMatch(Path.GetFileNameWithoutExtension(mp3Path), "^[0-9a-f]+$")) {
            var betterName = entry.Artists[0] + " - " + entry.Title;
            betterName = Regex.Replace(betterName, @"[^\p{L}\p{N} (.,&')]+", "-").Trim('-');
            File.Move(mp3Path, Path.Combine(
                Path.GetDirectoryName(mp3Path),
                betterName + ".mp3"
            ));
        }
    }

}
