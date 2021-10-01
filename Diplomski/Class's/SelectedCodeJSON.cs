using Newtonsoft.Json;
using System.Collections.Generic;

namespace Diplomski.JSON_Class
{
    class SelectedCodeJSON
    {
        public string mal_id { get; set; }
        public string url { get; set; }
        public string image_url { get; set; }
        public string trailer_url { get; set; }
        public string title { get; set; }
        public string title_english { get; set; }
        public string title_japanese { get; set; }
        public string type { get; set; }
        public string source { get; set; }
        public string episodes { get; set; }
        public string status { get; set; }
        public string duration { get; set; }
        public string rating { get; set; }
        public string score { get; set; }
        public string synopsis { get; set; }

        public Aired aired { get; set; }
        public class Aired
        {
            public string from { get; set; }
            public string to { get; set; }
        }

        public IList<string> opening_themes { get; set; }
        public IList<string> ending_themes { get; set; }

        public IList<Studios> studios { get; set; }
        public class Studios
        {
            public string name { get; set; }
        }

        public Related related { get; set; }
        public class Related
        {
            public IList<Prequel> Prequel { get; set; }
            public IList<Sequel> Sequel { get; set; }
            public IList<Summary> Summary { get; set; }
            public IList<Other> Other { get; set; }

            [JsonProperty("Alternative version")]
            public IList<Alternativeversion> Alternativeversion { get; set; }

            [JsonProperty("Alternative setting")]
            public IList<Alternativesetting> Alternativesetting { get; set; }

            [JsonProperty("Parent story")]
            public IList<Parentstory> Parentstory { get; set; }

            [JsonProperty("Side story")]
            public IList<Sidestory> Sidestory { get; set; }
        }

        public class Prequel
        {
            public string mal_id { get; set; }
            public string type { get; set; }
            public string name { get; set; }
            public string url { get; set; }
        }

        public class Sequel
        {
            public string mal_id { get; set; }
            public string type { get; set; }
            public string name { get; set; }
            public string url { get; set; }
        }

        public class Summary
        {
            public string mal_id { get; set; }
            public string type { get; set; }
            public string name { get; set; }
            public string url { get; set; }
        }


        public class Other
        {
            public string mal_id { get; set; }
            public string type { get; set; }
            public string name { get; set; }
            public string url { get; set; }
        }

        public class Alternativeversion
        {
            public string mal_id { get; set; }
            public string type { get; set; }
            public string name { get; set; }
            public string url { get; set; }
        }

        public class Alternativesetting
        {
            public string mal_id { get; set; }
            public string type { get; set; }
            public string name { get; set; }
            public string url { get; set; }
        }

        public class Parentstory
        {
            public string mal_id { get; set; }
            public string type { get; set; }
            public string name { get; set; }
            public string url { get; set; }
        }

        public class Sidestory
        {
            public string mal_id { get; set; }
            public string type { get; set; }
            public string name { get; set; }
            public string url { get; set; }
        }


        public IList<Genre> genres { get; set; }
        public class Genre
        {
            public string mal_id { get; set; }
            public string type { get; set; }
            public string name { get; set; }
            public string url { get; set; }
        }
    }
}
