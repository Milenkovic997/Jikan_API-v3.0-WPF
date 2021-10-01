namespace Diplomski.Class_s
{
    class SortAnimeClass
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public string Score { get; set; }
        public string Type { get; set; }
        public string Episodes { get; set; }
        public string Status { get; set; }

        public SortAnimeClass(string id, string title, string score, string type, string episodes, string status)
        {
            ID = id;
            Title = title;
            Score = score;
            Type = type;
            Episodes = episodes;
            Status = status;
        }
    }
}
