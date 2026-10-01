namespace NzbDrone.Core.MetadataSource.SkyHook.Resource
{
    public class RatingResource
    {
        // Series ratings use the flat Count/Value shape
        public int Count { get; set; }
        public decimal Value { get; set; }

        // Movie ratings use the per-source shape
        public RatingItem Tmdb { get; set; }
        public RatingItem Imdb { get; set; }
        public RatingItem Metacritic { get; set; }
        public RatingItem RottenTomatoes { get; set; }
        public RatingItem Trakt { get; set; }
    }

    public class RatingItem
    {
        public int Count { get; set; }
        public decimal Value { get; set; }
        public string Origin { get; set; }
        public string Type { get; set; }
    }
}
