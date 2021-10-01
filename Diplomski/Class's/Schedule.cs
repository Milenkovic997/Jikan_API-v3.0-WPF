using System;
using System.Collections.Generic;

namespace Diplomski.Class_s
{
    class Schedule
    {
        public int mal_id { get; set; }
        public string title { get; set; }
        public string image_url { get; set; }
        public string type { get; set; }
        public string airing_start { get; set; }
        public int? episodes { get; set; }
        public double? score { get; set; }
    }
}
