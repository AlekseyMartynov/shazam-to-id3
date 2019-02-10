using System;
using System.IO;
using System.Text.RegularExpressions;

class Program {

    static void Main(string[] args) {
        // args = new[] { "c:/temp/test.mp3", "shazam:379653703" };
        // args = new[] { "c:/temp/test.mp3", "spotify:track:3gAvyxokyDwmBzUPyl741m" };

        var mp3Path = args[0];
        var meta = ReadMetadata();

        TrackMetadata ReadMetadata() {
            var metaSource = args[1];

            const string shazamPrefix = "shazam:";
            if(metaSource.StartsWith(shazamPrefix))
                return ShazamReader.FromID(metaSource.Substring(shazamPrefix.Length));

            const string spotifyPrefix = "spotify:track:";
            if(metaSource.StartsWith(spotifyPrefix))
                return SpotifyReader.Read(metaSource.Substring(spotifyPrefix.Length));

            throw new NotSupportedException();
        }

        File.Copy(mp3Path, mp3Path + ".bak");

        using(var file = TagLib.File.Create(mp3Path)) {
            file.RemoveTags(TagLib.TagTypes.AllTags & ~TagLib.TagTypes.Id3v2);

            var id3 = (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2);
            id3.Clear();

            id3.Title = meta.Title;
            id3.Performers = meta.Artists;

            if(meta.Artwork != null) {
                id3.Pictures = new[] {
                    new TagLib.Id3v2.AttachedPictureFrame {
                        MimeType = meta.ArtworkMime,
                        Type = TagLib.PictureType.FrontCover,
                        Data = meta.Artwork,
                        TextEncoding = TagLib.StringType.Latin1
                    }
                };
            }

            if(!String.IsNullOrEmpty(meta.Album))
                id3.Album = meta.Album;

            if(!String.IsNullOrEmpty(meta.Label))
                id3.SetTextFrame("TPUB", meta.Label);

            if(meta.Year.HasValue)
                id3.Year = meta.Year.Value;

            if(!String.IsNullOrEmpty(meta.Genre))
                id3.Genres = new[] { meta.Genre };

            if(meta.TrackNumber > 0)
                id3.Track = meta.TrackNumber;

            if(!String.IsNullOrEmpty(meta.SourceUrl)) {
                var url = new TagLib.ByteVector { 0, "Metadata Source", 0, meta.SourceUrl };
                id3.AddFrame(new TagLib.Id3v2.UnknownFrame("WXXX", url));
            }

            file.Save();
        }

        var betterName = meta.Artists[0] + " - " + meta.Title;
        betterName = Regex.Replace(betterName, @"[^\p{L}\p{N} (.,&')]+", "-").Trim('-');
        File.Move(mp3Path, Path.Combine(
            Path.GetDirectoryName(mp3Path),
            betterName + ".mp3"
        ));
    }

}
