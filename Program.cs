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

        TagLib.IPicture CreatePicture() => new TagLib.Picture {
            Type = TagLib.PictureType.FrontCover,
            MimeType = meta.ArtworkMime,
            Data = meta.Artwork
        };

        void AssignID3(TagLib.Id3v2.Tag id3) {
            AssignStandardProps(id3);

            if(meta.Artwork != null) {
                id3.Pictures = new[] {
                    new TagLib.Id3v2.AttachedPictureFrame(CreatePicture()) {
                        TextEncoding = TagLib.StringType.Latin1
                    }
                };
            }

            if(!String.IsNullOrEmpty(meta.Label))
                id3.SetTextFrame("TPUB", meta.Label);

            var sourceUrl = new TagLib.ByteVector { 0, "Metadata Source", 0, meta.SourceUrl };
            id3.AddFrame(new TagLib.Id3v2.UnknownFrame("WXXX", sourceUrl));
        }

        void AssignFlac(TagLib.Flac.Metadata flac) {
            var xiph = flac.GetComment(true, null);

            AssignStandardProps(xiph);

            if(meta.Artwork != null)
                flac.Pictures = new[] { CreatePicture() };

            if(!String.IsNullOrEmpty(meta.Label))
                xiph.SetField("ORGANIZATION", meta.Label);

            xiph.SetField("Metadata Source", meta.SourceUrl);
        }

        TagLib.TagTypes GetTagType(TagLib.File file) {
            switch(file.MimeType) {
                case "taglib/mp3":
                    return TagLib.TagTypes.Id3v2;
                case "taglib/flac":
                    return TagLib.TagTypes.FlacMetadata;
            }
            throw new NotSupportedException();
        }

        using(var file = TagLib.File.Create(audioFilePath)) {
            var tagType = GetTagType(file);
            file.RemoveTags(TagLib.TagTypes.AllTags & ~tagType);

            var tag = file.GetTag(tagType);
            tag.Clear();

            switch(tag) {
                case TagLib.Id3v2.Tag id3:
                    AssignID3(id3);
                    break;
                case TagLib.Flac.Metadata flac:
                    AssignFlac(flac);
                    break;
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
