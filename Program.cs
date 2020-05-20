using System;
using System.IO;
using System.Text.RegularExpressions;

class Program {

    static void Main(string[] args) {
        // args = new[] { "c:/temp/test.mp3", "shazam:379653703" };
        // args = new[] { "c:/temp/test.mp3", "spotify:track:3gAvyxokyDwmBzUPyl741m" };

        var audioFilePath = args[0];
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

        File.Copy(audioFilePath, audioFilePath + ".bak");

        void AssignStandardProps(TagLib.Tag tag) {
            tag.Title = meta.Title;
            tag.Performers = meta.Artists;

            if(!String.IsNullOrEmpty(meta.Album))
                tag.Album = meta.Album;

            if(meta.Year.HasValue)
                tag.Year = meta.Year.Value;

            if(!String.IsNullOrEmpty(meta.Genre))
                tag.Genres = new[] { meta.Genre };

            if(meta.TrackNumber > 0)
                tag.Track = meta.TrackNumber;
        }

        using(var file = TagLib.File.Create(audioFilePath)) {
            file.RemoveTags(TagLib.TagTypes.AllTags & ~TagLib.TagTypes.Id3v2);

            var tag = (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2);
            tag.Clear();

            AssignStandardProps(tag);

            if(meta.Artwork != null) {
                tag.Pictures = new[] {
                    new TagLib.Id3v2.AttachedPictureFrame {
                        MimeType = meta.ArtworkMime,
                        Type = TagLib.PictureType.FrontCover,
                        Data = meta.Artwork,
                        TextEncoding = TagLib.StringType.Latin1
                    }
                };
            }

            if(!String.IsNullOrEmpty(meta.Label))
                tag.SetTextFrame("TPUB", meta.Label);

            if(!String.IsNullOrEmpty(meta.SourceUrl)) {
                var url = new TagLib.ByteVector { 0, "Metadata Source", 0, meta.SourceUrl };
                tag.AddFrame(new TagLib.Id3v2.UnknownFrame("WXXX", url));
            }

            file.Save();
        }

        var betterName = meta.Artists[0] + " - " + meta.Title;
        betterName = Regex.Replace(betterName, @"[^\p{L}\p{N} (.,&')]+", "-").Trim('-');
        File.Move(audioFilePath, Path.Combine(
            Path.GetDirectoryName(audioFilePath),
            betterName + Path.GetExtension(audioFilePath)
        ));
    }

}
