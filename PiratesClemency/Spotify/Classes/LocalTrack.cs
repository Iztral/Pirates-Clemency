﻿using System.Xml.Serialization;

namespace PiratesClemency.Spotify.Classes
{
    public enum TagState { TITLE_ONLY, MISSING_TAG, FULL_TAGS }

    [XmlRootAttribute("LocalTrack", Namespace = "Pirate's Clemency", IsNullable = false)]
    public class LocalTrack
    {
        public string File_name { get; set; }

        public string Author { get; set; }

        public string Title { get; set; }

        public string Path { get; set; }

        public TagState TagState { get; set; }

        public string SpotifyUri { get; set; }

        public string Error { get; set; }
    }
}
