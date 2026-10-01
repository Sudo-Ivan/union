using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NzbDrone.Core.ImportLists;
using System.Runtime.Serialization;

namespace NzbDrone.Core.ImportLists.Simkl
{
    public class SimklSeriesIdsResource
    {
        public int Simkl { get; set; }
        public string Slug { get; set; }
        public string Imdb { get; set; }
        public string Tmdb { get; set; }
        public string Tvdb { get; set; }
        public string Mal { get; set; }
    }

    public class SimklMovieIdsResource
    {
        public int Simkl { get; set; }
        public string Imdb { get; set; }
        public string Tmdb { get; set; }
        public string Mal { get; set; }
    }

    public class SimklSeriesPropsResource
    {
        public string Title { get; set; }
        public int? Year { get; set; }
        public SimklSeriesIdsResource Ids { get; set; }
    }

    public class SimklMoviePropsResource
    {
        public string Title { get; set; }
        public int? Year { get; set; }
        public SimklMovieIdsResource Ids { get; set; }
    }

    public class SimklSeriesResource
    {
        public SimklSeriesPropsResource Show { get; set; }

        [JsonProperty("anime_type")]
        public SimklAnimeType AnimeType { get; set; }
    }

    public class SimklMovieResource
    {
        public SimklMoviePropsResource Movie { get; set; }

        [JsonProperty("anime_type")]
        public SimklAnimeType AnimeType { get; set; }
    }

    public class SimklShowResource
    {
        public SimklMoviePropsResource Show { get; set; }

        [JsonProperty("anime_type")]
        public SimklAnimeType AnimeType { get; set; }
    }

    public class SimklResponse
    {
        public List<SimklSeriesResource> Shows { get; set; }
        public List<SimklMovieResource> Movies { get; set; }
        public List<SimklSeriesResource> Anime { get; set; }
    }

    public class RefreshRequestResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }

    public class UserSettingsResponse
    {
        public UserAccountResource Account { get; set; }
    }

    public class UserAccountResource
    {
        public int Id { get; set; }
    }

    public class SimklSyncActivityResource
    {
        [JsonProperty("tv_shows")]
        public SimklTvSyncActivityResource TvShows { get; set; }

        [JsonProperty("movies")]
        public SimklMoviesSyncActivityResource Movies { get; set; }

        [JsonProperty("anime")]
        public SimklTvSyncActivityResource Anime { get; set; }
    }

    public class SimklTvSyncActivityResource
    {
        public DateTime All { get; set; }
    }

    public class SimklMoviesSyncActivityResource
    {
        public DateTime All { get; set; }
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum SimklAnimeType
    {
        Unknown,
        Tv,
        Movie,
        Ova,
        Ona,
        Special,

        [EnumMember(Value = "music video")]
        MusicVideo
    }
}
